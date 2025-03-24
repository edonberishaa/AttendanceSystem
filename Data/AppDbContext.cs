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
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Subject>()
                .HasOne(s => s.Professor)
                .WithMany()
                .HasForeignKey(s => s.ProfessorID);

            builder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentID);

            builder.Entity<Attendance>()
                .HasOne(a => a.Subject)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.SubjectID);
        }

    }
}
