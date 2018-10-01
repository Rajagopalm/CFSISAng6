using Microsoft.EntityFrameworkCore;
using System;


namespace CFSISBusiness
{
    public class CFSISContext : DbContext
    {        
        public string ConnectionString { get; set; }

        public CFSISContext(DbContextOptions options) : base(options)
        {         
        }

        public DbSet<Album> Albums { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Student> Students { get; set; } 
        public DbSet<Academic> Academics { get; set; }               
        public DbSet<Enrollment> Enrollments { get; set; }   
          
        public DbSet<Semester> Semesters { get; set; }   

        public DbSet<College> Colleges { get; set; }    
        public DbSet<Course> Courses { get; set; }                          
        public DbSet<User> Users { get; set;  }

        
        protected override void OnModelCreating(ModelBuilder builder)
        {         
            base.OnModelCreating(builder);
        }


        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    base.OnConfiguring(optionsBuilder);

        //    if (optionsBuilder.IsConfigured)
        //        return;

        //    // Auto configuration
        //    ConnectionString = Configuration.GetValue<string>("Data:AlbumViewer:ConnectionString");
        //    optionsBuilder.UseSqlServer(ConnectionString);
        //}

    }
}