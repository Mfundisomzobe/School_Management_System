using Microsoft.AspNetCore.Identity;
using School_Management_System.Models;

namespace School_Management_System.Data
{
    public static class DbInitializer
    {
         public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context =serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager =serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager =serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();


            //Ensure database is created
            await context.Database.EnsureCreatedAsync();

            //Create roles if they don't exists

            string[] roleNames = { "Admin", "Teacher", "Student", "Parent" };

            foreach(var roleName in roleNames)
            { 
                if(!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            //Create admin user if it doesn't exist
            var adminEmail = "admin@school.com";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if(adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName= "System Administrator",
                    Role= "Admin",
                    CreatedAt= DateTime.UtcNow,
                    IsActive= true,
                    EmailConfirmed= true //Auto confirmation

                };

                var createResult = await userManager.CreateAsync(user, "Admin@123");

                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }
        }
        
    }
}
