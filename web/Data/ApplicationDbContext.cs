using HomeworkPlatform.Web.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace HomeworkPlatform.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<StudentCourse> StudentCourses => Set<StudentCourse>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Course>().ToTable("Courses", table => table.HasCheckConstraint("CK_Courses_EducationGrade",
            "(EducationLevel = 0 AND GradeLevel BETWEEN 9 AND 12) OR (EducationLevel = 1 AND GradeLevel BETWEEN 1 AND 4)"));
        builder.Entity<Course>().Property(c => c.IsActive).HasDefaultValue(true);
        builder.Entity<Course>().HasOne<ApplicationUser>().WithMany().HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Assignment>().HasOne(a => a.Course).WithMany().HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StudentCourse>().HasKey(e => new { e.StudentId, e.CourseId });
        builder.Entity<StudentCourse>().HasOne(e => e.Course).WithMany().HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<StudentCourse>().HasOne<ApplicationUser>().WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Submission>().HasOne(s => s.Assignment).WithMany().HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Submission>().HasOne<ApplicationUser>().WithMany().HasForeignKey(s => s.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Submission>().HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
    }
}
