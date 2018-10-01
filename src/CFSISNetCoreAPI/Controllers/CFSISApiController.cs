using CFSISBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;


// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace CFSISNetCoreAPI
{
    [ServiceFilter(typeof(ApiExceptionFilter))]
    [EnableCors("CorsPolicy")]
    public class CFSISApiController : Controller
    {
        CFSISContext context;
        IServiceProvider serviceProvider;

        ArtistRepository ArtistRepo;
        AlbumRepository AlbumRepo;
        CollegeRepository CollegeRepo;
        CourseRepository CourseRepo;
        DistrictRepository DistrictRepo;
        EnrollmentRepository EnrollmentRepo;
        StudentRepository StudentRepo;


        IConfiguration Configuration;
        private ILogger<CFSISApiController> Logger;

        private IHostingEnvironment HostingEnv;

        public CFSISApiController(
            CFSISContext ctx,
            IServiceProvider svcProvider,
            ArtistRepository artistRepo,
            AlbumRepository albumRepo,
            CollegeRepository collegeRepo,
            CourseRepository courseRepo,
            DistrictRepository districtRepo,
            EnrollmentRepository enrollmentRepo,
            StudentRepository studentRepo,
            IConfiguration config,
            ILogger<CFSISApiController> logger,
            IHostingEnvironment env)
        {
            context = ctx;
            serviceProvider = svcProvider;
            Configuration = config;

            AlbumRepo = albumRepo;
            ArtistRepo = artistRepo;
            CollegeRepo = collegeRepo;
            CourseRepo = courseRepo;
            DistrictRepo = districtRepo;
            EnrollmentRepo = enrollmentRepo;
            StudentRepo = studentRepo;
            Logger = logger;

            HostingEnv = env;
        }




        [HttpGet]
        [Route("api/throw")]
        public object Throw()
        {
            throw new InvalidOperationException("This is an unhandled exception");
        }


        #region albums

        [HttpGet]
        [Route("api/albums")]
        public async Task<IEnumerable<Album>> GetAlbums(int page = -1, int pageSize = 15)
        {
            //var repo = new AlbumRepository(context);
            return await AlbumRepo.GetAllAlbums(page, pageSize);
        }

        [HttpGet("api/album/{id:int}")]
        public async Task<Album> GetAlbum(int id)
        {
            return await AlbumRepo.Load(id);
        }

        [HttpPost("api/album")]
        public async Task<Album> SaveAlbum([FromBody] Album postedAlbum)
        {
            //throw new ApiException("Lemmy says: NO!");

            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            if (!ModelState.IsValid)
                throw new ApiException("Model binding failed.", 500);

            if (!AlbumRepo.Validate(postedAlbum))
                throw new ApiException(AlbumRepo.ErrorMessage, 500, AlbumRepo.ValidationErrors);

            // this doesn't work for updating the child entities properly
            //if(!await AlbumRepo.SaveAsync(postedAlbum))
            //    throw new ApiException(AlbumRepo.ErrorMessage, 500);

            var album = await AlbumRepo.SaveAlbum(postedAlbum);
            if (album == null)
                throw new ApiException(AlbumRepo.ErrorMessage, 500);

            return album;
        }

        [HttpDelete("api/album/{id:int}")]
        public async Task<bool> DeleteAlbum(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            return await AlbumRepo.DeleteAlbum(id);
        }


        [HttpGet]
        public async Task<string> DeleteAlbumByName(string name)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            var pks =
                await context.Albums.Where(alb => alb.Title == name).Select(alb => alb.Id).ToAsyncEnumerable().ToList();

            StringBuilder sb = new StringBuilder();
            foreach (int pk in pks)
            {
                bool result = await AlbumRepo.DeleteAlbum(pk);
                if (!result)
                    sb.AppendLine(AlbumRepo.ErrorMessage);
            }

            return sb.ToString();
        }

        #endregion

        #region artists

        [HttpGet]
        [Route("api/artists")]
        public async Task<IEnumerable> GetArtists()
        {
            return await ArtistRepo.GetAllArtists();
        }

        [HttpGet("api/artist/{id:int}")]
        public async Task<object> Artist(int id)
        {
            var artist = await ArtistRepo.Load(id);

            if (artist == null)
                throw new ApiException("Invalid artist id.", 404);

            var albums = await ArtistRepo.GetAlbumsForArtist(id);

            return new ArtistResponse()
            {
                Artist = artist,
                Albums = albums
            };
        }

        [HttpPost("api/artist")]
        public async Task<ArtistResponse> SaveArtist([FromBody] Artist artist)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            if (!ArtistRepo.Validate(artist))
                throw new ApiException(ArtistRepo.ValidationErrors.ToString(), 500, ArtistRepo.ValidationErrors);

            if (!await ArtistRepo.SaveAsync(artist))
                throw new ApiException("Unable to save artist.");

            return new ArtistResponse()
            {
                Artist = artist,
                Albums = await ArtistRepo.GetAlbumsForArtist(artist.Id)
            };
        }

        [HttpGet("api/artistlookup")]
        public async Task<IEnumerable<object>> ArtistLookup(string search = null)
        {
            if (string.IsNullOrEmpty(search))
                return new List<object>();

            var repo = new ArtistRepository(context);
            var term = search.ToLower();
            return await repo.ArtistLookup(term);
        }


        [HttpDelete("api/artist/{id:int}")]
        public async Task<bool> DeleteArtist(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            return await ArtistRepo.DeleteArtist(id);
        }

        #endregion


        #region admin
        [HttpGet]
        [Route("api/reloaddata")]
        public bool ReloadData()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            string isSqLite = Configuration["data:useSqLite"];
            try
            {
                if (isSqLite != "true")
                {
                    context.Database.ExecuteSqlCommand(@"
                    drop table Tracks;
                    drop table Albums;
                    drop table Artists;
                    drop table Users;
                    ");
                }
                else
                {
                    // this is not reliable for mutliple connections
                    context.Database.CloseConnection();

                    try
                    {
                        System.IO.File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "AlbumViewerData.sqlite"));
                    }
                    catch
                    {
                        throw new ApiException("Can't reset data. Existing database is busy.");
                    }
                }

            }
            catch { }


            CFSISDataImporter.EnsureAlbumData(context,
                Path.Combine(HostingEnv.ContentRootPath,
                "albums.js"));

            return true;
        }


        #endregion

        #region college
        /*
        [HttpGet]
        [Route("api/colleges")]
        public async Task<IEnumerable<College>> Getcolleges(int page = -1, int pageSize = 15)
        {
            //var repo = new CollegeRepository(context);
            return await CollegeRepo.GetAllColleges(page, pageSize);
        }
        */
        [HttpGet("api/college/{id:int}")]
        public async Task<College> GetCollege(int id)
        {
            return await CollegeRepo.Load(id);
        }
        /*
        [HttpPost("api/college")]
        public async Task<College> SaveCollege([FromBody] College postedCollege)
        {
            //throw new ApiException("Lemmy says: NO!");

            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            if (!ModelState.IsValid)
                throw new ApiException("Model binding failed.", 500);

            if (!CollegeRepo.Validate(postedCollege))
                throw new ApiException(CollegeRepo.ErrorMessage, 500, CollegeRepo.ValidationErrors);

            // this doesn't work for updating the child entities properly
            //if(!await CollegeRepo.SaveAsync(postedCollege))
            //    throw new ApiException(CollegeRepo.ErrorMessage, 500);

            var college = await CollegeRepo.SaveCollege(postedCollege);
            if (college == null)
                throw new ApiException(CollegeRepo.ErrorMessage, 500);

            return college;
        }
        */
        [HttpDelete("api/college/{id:int}")]
        public async Task<bool> Deletecollege(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            return await CollegeRepo.DeleteCollege(id);
        }


        [HttpGet]
        public async Task<string> DeleteCollegeByName(string name)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            var pks =
                await context.Colleges.Where(clg => clg.CollegeName == name).Select(clg => clg.CollegeId).ToAsyncEnumerable().ToList();

            StringBuilder sb = new StringBuilder();
            foreach (int pk in pks)
            {
                bool result = await CollegeRepo.DeleteCollege(pk);
                if (!result)
                    sb.AppendLine(CollegeRepo.ErrorMessage);
            }

            return sb.ToString();
        }

        #endregion

        #region districts

        [HttpGet]
        [Route("api/districts")]
        public async Task<IEnumerable> GetDistricts()
        {
            return await DistrictRepo.GetAllDistricts();
        }

        [HttpGet("api/districts/{id:int}")]
        public async Task<object> District(int id)
        {
            var district = await DistrictRepo.Load(id);

            if (district == null)
                throw new ApiException("Invalid district id.", 404);

            var students = await DistrictRepo.GetStudentsForDistrict(id);

            return new DistrictResponse()
            {
                District = district,
                Students = students
            };
        }

        [HttpPost("api/district")]
        public async Task<DistrictResponse> SaveDistrict([FromBody] District district)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            if (!DistrictRepo.Validate(district))
                throw new ApiException(DistrictRepo.ValidationErrors.ToString(), 500, DistrictRepo.ValidationErrors);

            if (!await DistrictRepo.SaveAsync(district))
                throw new ApiException("Unable to save district.");

            return new DistrictResponse()
            {
                District = district,
                Students = await DistrictRepo.GetStudentsForDistrict(district.DistrictId)
            };
        }

        [HttpGet("api/districtlookup")]
        public async Task<IEnumerable<object>> DistrictLookup(string search = null)
        {
            if (string.IsNullOrEmpty(search))
                return new List<object>();

            var repo = new DistrictRepository(context);
            var term = search.ToLower();
            return await repo.DistrictLookup(term);
        }


        [HttpDelete("api/district/{id:int}")]
        public async Task<bool> DeleteDistrict(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            return await DistrictRepo.DeleteDistrict(id);
        }

        #endregion
    }

    #region Custom Responses

    public class ArtistResponse
    {
        public Artist Artist { get; set; }

        public List<Album> Albums { get; set; }
    }

    public class DistrictResponse
    {
        public District District { get; set; }

        public List<Student> Students { get; set; }
    }

    #endregion
}

