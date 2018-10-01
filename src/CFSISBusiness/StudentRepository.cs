
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Westwind.BusinessObjects;
using Westwind.Utilities;

namespace CFSISBusiness
{
    public class StudentRepository : EntityFrameworkRepository<CFSISContext, Student>
    {
        public StudentRepository(CFSISContext context)
            : base(context)
        { }


        /// <summary>
        /// Loads and individual student.
        /// 
        /// Implementation is custom not using base.Load()
        /// in order to include related entities
        /// </summary>
        /// <param name="objId">Student Id</param>
        /// <returns></returns>
        public override async Task<Student> Load(object studentId)
        {
            Student student = null;
            try
            {
                int id = (int)studentId;
                student = await Context.Students
                    .Include(ctx => ctx.Enrollments)
                    .Include(ctx => ctx.Academic)
                    .FirstOrDefaultAsync(std => std.StudentId == id);

                if (student != null)
                    OnAfterLoaded(student);
            }
            catch (InvalidOperationException)
            {
                // Handles errors where an invalid Id was passed, but SQL is valid                
                SetError("Couldn't load student - invalid student id specified.");
                return null;
            }
            catch (Exception ex)
            {
                // handles Sql errors                                
                SetError(ex);
            }

            return student;
        }



        public async Task<List<Student>> GetAllStudents(int page = 0, int pageSize = 15)
        {
            IQueryable<Student> students = Context.Students
                .Include(ctx => ctx.Enrollments)
                .Include(ctx => ctx.Academic)
                .OrderBy(std => std.FirstName);

            if (page > 0)
            {
                students = students
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize);
            }

            return await students.ToListAsync();
        }

        /// <summary>
        /// This code is rather complex as EF7 can't work out
        /// the related entity updates for artist and tracks, 
        /// so this code manually  updates artists and tracks 
        /// from the saved entity using code.
        /// </summary>
        /// <param name="postedStudent"></param>
        /// <returns></returns>
        public async Task<Student> SaveStudent(Student postedStudent)
        {
            int id = postedStudent.StudentId;

            Student student;

            if (id < 1)
                student = Create();
            else
            {
                student = await Load(id);
                if (student == null)
                    student = Create();
            }

            // new academic 
            if (student.Academic.AcademicId < 1)
                Context.Academics.Add(student.Academic);


            // add or udpate tracks
            foreach (var postedEnrollment in postedStudent.Enrollments)
            {
                var enrollment = student.Enrollments.FirstOrDefault(trk => trk.EnrollmentId == postedEnrollment.EnrollmentId);
                if (postedEnrollment.EnrollmentId > 0 && enrollment != null)
                    DataUtils.CopyObjectData(postedEnrollment, enrollment);
                else
                {
                    enrollment = new Enrollment();
                    Context.Enrollments.Add(enrollment);
                    DataUtils.CopyObjectData(postedEnrollment, enrollment, "EnrollmentId,StudentId");
                    student.Enrollments.Add(enrollment);
                }
            }


            // then find all deleted tracks not in new tracks
            var deletedEnrollments = student.Enrollments
                .Where(trk => trk.EnrollmentId > 0 &&
                                !postedStudent.Enrollments
                                    .Where(t => t.EnrollmentId > 0)
                                    .Select(t => t.EnrollmentId)
                                .Contains(trk.EnrollmentId))
                .ToList();

            foreach (var dtrack in deletedEnrollments)
                student.Enrollments.Remove(dtrack);

            //now lets save it all
            if (!await SaveAsync())
                return null;

            return student;
        }


        public async Task<bool> DeleteStudent(int id, IDbContextTransaction tx = null)
        {
            bool nested = false;
            if (tx == null)
                tx = Context.Database.BeginTransaction();
            else
                nested = true;

            // manually delete enrollments
            var enrollments = await Context.Enrollments.Where(t => t.StudentId == id).ToListAsync();
            for (int i = enrollments.Count - 1; i > -1; i--)
            {
                var enrollment = enrollments[i];
                enrollments.Remove(enrollment);
                Context.Enrollments.Remove(enrollment);
            }

            var student = await Context.Students
                .FirstOrDefaultAsync(a => a.StudentId == id);

            if (student == null)
            {
                SetError("Invalid student id.");

                return false;
            }

            Context.Students.Remove(student);

            var result = await SaveAsync();

            if (result && !nested)
                tx.Commit();
            
            if (!nested)
                tx.Dispose();

            return result;
        }


        protected override bool OnValidate(Student entity)
        {
            if (entity == null)
            {
                ValidationErrors.Add("No item was passed.");
                return false;
            }

            if (string.IsNullOrEmpty(entity.FirstName))
                ValidationErrors.Add("Please enter a Name for this student.", "FirstName");
            else if (entity.Enrollments.Count < 1)
                ValidationErrors.Add("Student must have at least one song associated.");

            return ValidationErrors.Count < 1;
        }

    }

}