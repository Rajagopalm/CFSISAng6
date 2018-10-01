
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.BusinessObjects;
using Westwind.Utilities;

namespace CFSISBusiness
{
public class CollegeRepository : EntityFrameworkRepository<CFSISContext,College>
{    
    public CollegeRepository(CFSISContext context)
        : base(context)
    { }
        
    /// <summary>
    /// Returns a list of students from all colleges excluding Schools
    /// </summary>        
/*     public async Task<List<CollegeWithStudentCount>> GetAllColleges()
    {
        return await Context.Colleges
            .OrderBy(dst => dst.CollegeName)
            .Select(dst => new CollegeWithStudentCount()
            {
                CollegeName = dst.CollegeName,
                CollegeId = dst.CollegeId,
                StudentCount = Context.Students.Count(std => std.CollegeId == dst.CollegeId)
            })
            .ToListAsync();
    } */

    /// <summary>
    /// Returns a list of students for a given college
    /// </summary>
    /// <param name="collegeId"></param>
    /// <returns></returns>
    public async Task<List<Student>> GetStudentsForCollege(int collegeId)
    {
        var studentIds = await Context.Enrollments
        .Where(x => x.CollegeId == collegeId) // filtering goes here
        .Select(x => x.StudentId)
        .Distinct()
        .ToListAsync();

        return await Context.Students
        .Include(a => a.StudentId)
        .Where(x => studentIds.Contains(x.StudentId))
        .ToListAsync();
        /* 
        return await Context.Students
            .Include(a => a.CollegeId)
            .Where(a => a.CollegeId == collegeId)
            .ToListAsync();
            */
    } 


    /// <summary>
    /// College look up by name - used for auto-complete box returns
    /// </summary>
    /// <param name="search"></param>
    /// <returns></returns>
    public async Task<List<CollegeLookupItem>> CollegeLookup(string search = null)
    {
        if (string.IsNullOrEmpty(search))
            return new List<CollegeLookupItem>();

        var repo = new CollegeRepository(Context);

        var term = search.ToLower();
        return await repo.Context.Colleges
            .Where(a => a.CollegeName.ToLower().StartsWith(term))
            .Select(a => new CollegeLookupItem
            {
                name = a.CollegeName,
                id = a.CollegeId
            })
            .ToListAsync();
    }

 

    public async Task<bool> DeleteCollege(int id)
    {
        bool result = false;
        using (var tx = Context.Database.BeginTransaction())
        {
            var college = await Context.Colleges.FirstOrDefaultAsync(dst => dst.CollegeId == id);

            // already gone
            if (college == null)
                return true;

            var enrollmentIds = await Context.Enrollments.Where(std => std.CollegeId == id).Select(std => std.CollegeId).ToListAsync();

            if (enrollmentIds.Capacity > 0) // there are some Enrollment using this college, so cannot delete college
                return false;

            Context.Colleges.Remove(college);

            result = await SaveAsync(); // just save
            if (!result)
               return false;

            tx.Commit();

            return result;
        }
    }

    protected override bool OnValidate(College entity)
    {
        if (entity == null)
        {
            ValidationErrors.Add("No college to validate.");
            return false;
        }

        if (string.IsNullOrEmpty(entity.CollegeName))
            ValidationErrors.Add("Please enter a college name.","CollegeName");

        return ValidationErrors.Count == 0;
    }

/*         public Task SaveCollege(College postedCollege)
        {
            throw new NotImplementedException();
        } */
    }

    public class CollegeLookupItem
    {
        public string name { get; set; }
        public int id { get; set; }
    }
}