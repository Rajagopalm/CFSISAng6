
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.BusinessObjects;
using Westwind.Utilities;

namespace CFSISBusiness
{
public class CourseRepository : EntityFrameworkRepository<CFSISContext,Course>
{    
    public CourseRepository(CFSISContext context)
        : base(context)
    { }
        
    /// <summary>
    /// Returns a list of students from all courses excluding Schools
    /// </summary>        
/*     public async Task<List<CourseWithStudentCount>> GetAllCourses()
    {
        return await Context.Courses
            .OrderBy(dst => dst.CourseName)
            .Select(dst => new CourseWithStudentCount()
            {
                CourseName = dst.CourseName,
                CourseId = dst.CourseId,
                StudentCount = Context.Students.Count(std => std.CourseId == dst.CourseId)
            })
            .ToListAsync();
    } */

    /// <summary>
    /// Returns a list of students for a given course
    /// </summary>
    /// <param name="courseId"></param>
    /// <returns></returns>
    public async Task<List<Student>> GetStudentsForCourse(int courseId)
    {

        var studentIds = await Context.Enrollments
        .Where(x => x.CourseId == courseId) // filtering goes here
        .Select(x => x.StudentId)
        .Distinct()
        .ToListAsync();

        return await Context.Students
        .Include(a => a.StudentId)
        .Where(x => studentIds.Contains(x.StudentId))
        .ToListAsync();


        /* 
        return await Context.Students
            .Include(a => a.CourseId)
            .Where(a => a.CourseId == courseId)
            .ToListAsync();
            */
    } 


    /// <summary>
    /// Course look up by name - used for auto-complete box returns
    /// </summary>
    /// <param name="search"></param>
    /// <returns></returns>
    public async Task<List<CourseLookupItem>> CourseLookup(string search = null)
    {
        if (string.IsNullOrEmpty(search))
            return new List<CourseLookupItem>();

        var repo = new CourseRepository(Context);

        var term = search.ToLower();
        return await repo.Context.Courses
            .Where(a => a.CourseName.ToLower().StartsWith(term))
            .Select(a => new CourseLookupItem
            {
                name = a.CourseName,
                id = a.CourseId
            })
            .ToListAsync();
    }

 

    public async Task<bool> DeleteCourse(int id)
    {
        bool result = false;
        using (var tx = Context.Database.BeginTransaction())
        {
            var course = await Context.Courses.FirstOrDefaultAsync(dst => dst.CourseId == id);

            // already gone
            if (course == null)
                return true;

            var enrollmentIds = await Context.Enrollments.Where(std => std.CourseId == id).Select(std => std.CourseId).ToListAsync();

            if (enrollmentIds.Capacity > 0) // there are some Enrollment using this course, so cannot delete course
                return false;

            Context.Courses.Remove(course);

            result = await SaveAsync(); // just save
            if (!result)
               return false;

            tx.Commit();

            return result;
        }
    }

    protected override bool OnValidate(Course entity)
    {
        if (entity == null)
        {
            ValidationErrors.Add("No course to validate.");
            return false;
        }

        if (string.IsNullOrEmpty(entity.CourseName))
            ValidationErrors.Add("Please enter a course name.","CourseName");

        return ValidationErrors.Count == 0;
    }

}

    public class CourseLookupItem
    {
        public string name { get; set; }
        public int id { get; set; }
    }
}