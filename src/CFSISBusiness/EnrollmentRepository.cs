
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
    public class EnrollmentRepository : EntityFrameworkRepository<CFSISContext, Enrollment>
    {
        public EnrollmentRepository(CFSISContext context)
            : base(context)
        { }


        /// <summary>
        /// Loads and individual enrollment.
        /// 
        /// Implementation is custom not using base.Load()
        /// in order to include related entities
        /// </summary>
        /// <param name="objId">Enrollment Id</param>
        /// <returns></returns>
        public override async Task<Enrollment> Load(object enrollmentId)
        {
            Enrollment enrollment = null;
            try
            {
                int id = (int)enrollmentId;
                enrollment = await Context.Enrollments
                    .Include(ctx => ctx.Semesters)
                    //.Include(ctx => ctx.Payments)
                    .FirstOrDefaultAsync(enr => enr.EnrollmentId == id);

                if (enrollment != null)
                    OnAfterLoaded(enrollment);
            }
            catch (InvalidOperationException)
            {
                // Handles errors where an invalid Id was passed, but SQL is valid                
                SetError("Couldn't load enrollment - invalid enrollment id specified.");
                return null;
            }
            catch (Exception ex)
            {
                // handles Sql errors                                
                SetError(ex);
            }

            return enrollment;
        }



        public async Task<List<Enrollment>> GetAllEnrollments(int page = 0, int pageSize = 15)
        {
            IQueryable<Enrollment> enrollments = Context.Enrollments
                .Include(ctx => ctx.Semesters)
                //.Include(ctx => ctx.Payments)
                .OrderBy(enr => enr.EnrollmentId);

            if (page > 0)
            {
                enrollments = enrollments
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize);
            }

            return await enrollments.ToListAsync();
        }

        /// <summary>
        /// This code is rather complex as EF7 can't work out
        /// the related entity updates for artist and tracks, 
        /// so this code manually  updates artists and tracks 
        /// from the saved entity using code.
        /// </summary>
        /// <param name="postedEnrollment"></param>
        /// <returns></returns>
        public async Task<Enrollment> SaveEnrollment(Enrollment postedEnrollment)
        {
            int id = postedEnrollment.EnrollmentId;

            Enrollment enrollment;

            if (id < 1)
                enrollment = Create();
            else
            {
                enrollment = await Load(id);
                if (enrollment == null)
                    enrollment = Create();
            }

            // add or udpate semesters
            foreach (var postedSemester in postedEnrollment.Semesters)
            {
                var semester = enrollment.Semesters.FirstOrDefault(sem => sem.SemesterId == postedSemester.SemesterId);
                if (postedSemester.SemesterId > 0 && semester != null)
                    DataUtils.CopyObjectData(postedSemester, semester);
                else
                {
                    semester = new Semester();
                    Context.Semesters.Add(semester);
                    DataUtils.CopyObjectData(postedSemester, semester, "SemesterId,EnrollmentId");
                    enrollment.Semesters.Add(semester);
                }
            }


            // then find all deleted tracks not in new tracks
            var deletedSemesters = enrollment.Semesters
                .Where(sem => sem.SemesterId > 0 &&
                                !postedEnrollment.Semesters
                                    .Where(t => t.SemesterId > 0)
                                    .Select(t => t.SemesterId)
                                .Contains(sem.SemesterId))
                .ToList();

            foreach (var dsemester in deletedSemesters)
                enrollment.Semesters.Remove(dsemester);

            //now lets save it all
            if (!await SaveAsync())
                return null;

            return enrollment;
        }


        public async Task<bool> DeleteEnrollment(int id, IDbContextTransaction tx = null)
        {
            bool nested = false;
            if (tx == null)
                tx = Context.Database.BeginTransaction();
            else
                nested = true;

            // manually delete tracks
            var semesters = await Context.Semesters.Where(t => t.EnrollmentId == id).ToListAsync();
            for (int i = semesters.Count - 1; i > -1; i--)
            {
                var semester = semesters[i];
                semesters.Remove(semester);
                Context.Semesters.Remove(semester);
            }

            var enrollment = await Context.Enrollments
                .FirstOrDefaultAsync(a => a.EnrollmentId == id);

            if (enrollment == null)
            {
                SetError("Invalid enrollment id.");

                return false;
            }

            Context.Enrollments.Remove(enrollment);

            var result = await SaveAsync();

            if (result && !nested)
                tx.Commit();
            
            if (!nested)
                tx.Dispose();

            return result;
        }


        protected override bool OnValidate(Enrollment entity)
        {
            if (entity == null)
            {
                ValidationErrors.Add("No item was passed.");
                return false;
            }
            return ValidationErrors.Count < 1;
        }

    }

}