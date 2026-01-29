using CourseCenter.Api.Users;
using CourseCenter.Api.Users.Roles;
using Microsoft.AspNetCore.Identity;
using CourseCenter.Api.Categories;


namespace CourseCenter.Api
{
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Name = "Admin" },
                    new Role { Name = "Employee" }
                );

                context.SaveChanges();
            }

            if (!context.Users.Any())
            {
                var hasher = new PasswordHasher<User>();

                var adminUser = new User
                {
                    FullName = "System Admin",
                    Email = "admin@coursecenter.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                adminUser.PasswordHash =
                    hasher.HashPassword(adminUser, "Admin@123");

                context.Users.Add(adminUser);
                context.SaveChanges();

                var adminRole = context.Roles.First(r => r.Name == "Admin");

                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });

                context.SaveChanges();
            }
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Programming" },
                    new Category { Name = "Design" },
                    new Category { Name = "Marketing" },
                    new Category { Name = "Data" }
                );

                context.SaveChanges();
            }
        }
    }
}
