
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.BusinessObjects;
using Westwind.Utilities;

namespace CFSISBusiness
{
public class DistrictRepository : EntityFrameworkRepository<CFSISContext,District>
{    
    public DistrictRepository(CFSISContext context)
        : base(context)
    { }
        
    public async Task<List<DistrictWithStudentCount>> GetAllDistricts()
    {
        return await Context.Districts
            .OrderBy(dst => dst.DistrictName)
            .Select(dst => new DistrictWithStudentCount()
            {
                DistrictName = dst.DistrictName,
                DistrictId = dst.DistrictId,
                StudentCount = Context.Students.Count(std => std.DistrictId == dst.DistrictId)
            })
            .ToListAsync();
    }

    /// <summary>
    /// Returns a list of students for a given district
    /// </summary>
    /// <param name="districtId"></param>
    /// <returns></returns>
    public async Task<List<Student>> GetStudentsForDistrict(int districtId)
    {
        return await Context.Students
            .Include(a => a.DistrictId)
            .Where(a => a.DistrictId == districtId)
            .ToListAsync();
    }


    /// <summary>
    /// District look up by name - used for auto-complete box returns
    /// </summary>
    /// <param name="search"></param>
    /// <returns></returns>
    public async Task<List<DistrictLookupItem>> DistrictLookup(string search = null)
    {
        if (string.IsNullOrEmpty(search))
            return new List<DistrictLookupItem>();

        var repo = new StudentRepository(Context);

        var term = search.ToLower();
        return await repo.Context.Districts
            .Where(a => a.DistrictName.ToLower().StartsWith(term))
            .Select(a => new DistrictLookupItem
            {
                name = a.DistrictName,
                id = a.DistrictName
            })
            .ToListAsync();
    }

 

    public async Task<bool> DeleteDistrict(int id)
    {
        bool result = false;
        using (var tx = Context.Database.BeginTransaction())
        {
            var district = await Context.Districts.FirstOrDefaultAsync(dst => dst.DistrictId == id);

            // already gone
            if (district == null)
                return true;

            var studentIds = await Context.Students.Where(std => std.DistrictId == id).Select(std => std.DistrictId).ToListAsync();

            var studentRepo = new StudentRepository(Context);

            foreach (var studentId in studentIds)
            {
                // don't run async or we get p
                result = await studentRepo.DeleteStudent(studentId, tx);
                if (!result)
                    return false;
            }

            Context.Districts.Remove(district);

            result = await SaveAsync(); // just save
            if (!result)
               return false;

            tx.Commit();

            return result;
        }
    }

    protected override bool OnValidate(District entity)
    {
        if (entity == null)
        {
            ValidationErrors.Add("No district to validate.");
            return false;
        }

        if (string.IsNullOrEmpty(entity.DistrictName))
            ValidationErrors.Add("Please enter a district name.","DistrictName");

        return ValidationErrors.Count == 0;
    }

}

    public class DistrictLookupItem
    {
        public string name { get; set; }
        public string id { get; set; }
    }
}