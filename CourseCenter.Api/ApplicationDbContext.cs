using CourseCenter.Api.Assessment;
using CourseCenter.Api.Categories;
using CourseCenter.Api.Courseclasses;
using CourseCenter.Api.Courses;
using CourseCenter.Api.Enrollments;
using CourseCenter.Api.Leads;
using CourseCenter.Api.Payments;
using CourseCenter.Api.Students;
using CourseCenter.Api.Users;
using CourseCenter.Api.Users.Auth;
using CourseCenter.Api.Users.Roles;
using Microsoft.EntityFrameworkCore;






namespace CourseCenter.Api
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ===== DbSets =====
        public DbSet<Student> Students { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadNote> LeadNotes { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ArchivedEnrollment> ArchivedEnrollments { get; set; }
        public DbSet<ArchivedStudent> ArchivedStudents { get; set; }
        public DbSet<AssessmentAttempt> AssessmentAttempts { get; set; }
        public DbSet<AssessmentUserAnswer> AssessmentUserAnswers { get; set; }
        public DbSet<AssessmentTest> AssessmentTests { get; set; }
        public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; }
        public DbSet<AssessmentAnswer> AssessmentAnswers { get; set; }
        public DbSet<AssessmentResultRange> AssessmentResultRanges { get; set; }
        public DbSet<AssessmentAttemptAnswer> AssessmentAttemptAnswers { get; set; }
        public DbSet<CourseClass> CourseClasses { get; set; }
        public DbSet<ArchivedCourseClass> ArchivedCourseClasses { get; set; }





        // ===== Model Config =====
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserRole (Many-to-Many)
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lead>()
                .Property(l => l.Status)
                .HasConversion<string>();
            modelBuilder.Entity<LeadNote>()
                .HasOne(n => n.Lead)
                .WithMany()
                .HasForeignKey(n => n.LeadId);

            modelBuilder.Entity<LeadNote>()
                .HasOne(n => n.CreatedByUser)
                .WithMany()
                .HasForeignKey(n => n.CreatedByUserId);
            modelBuilder.Entity<AssessmentAnswer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

        }

    }
}
