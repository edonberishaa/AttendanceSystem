using AttendanceSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendanceSystem.Data
{
    public class AppDbContext:IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        { }
        public AppDbContext()
        {
        }
        public virtual DbSet<Subject> Subjects { get; set; }
        public virtual DbSet<Student> Students { get; set; }
        public virtual DbSet<Attendance> Attendances { get; set; }
        public  virtual DbSet<SessionState> SessionStates { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relationship between Subject and Professor
            builder.Entity<Subject>()
                .HasOne(s => s.Professor)
                .WithMany()
                .HasForeignKey(s => s.ProfessorID);

            // Relationship between Attendance and Student
            builder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentID);

            // Relationship between Attendance and Subject
            builder.Entity<Attendance>()
                .HasOne(a => a.Subject)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.SubjectID);

            // Relationship between SessionState and Subject
            builder.Entity<SessionState>()
                .HasOne(ss => ss.Subject) // SessionState has one Subject
                .WithMany()               // Subject has many SessionStates
                .HasForeignKey(ss => ss.SubjectID); // Foreign key is SubjectID
        }

    }
}
