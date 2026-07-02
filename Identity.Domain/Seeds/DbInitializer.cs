using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.Seeds
{
    public class DbInitializer
    {
        public static async Task SeedSuperAdmin(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");
            if (superAdminRole == null)
            {
                superAdminRole = new ApplicationRole { Name = "SuperAdmin", NormalizedName = "SUPERADMIN" };
                await roleManager.CreateAsync(superAdminRole);
            }

            var user = await userManager.FindByEmailAsync("superadmin@example.com");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "superadmin",
                    Email = "superadmin@example.com",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Admin@123"); // strong password

                await userManager.AddToRoleAsync(user, "SuperAdmin");
            }
        }
        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole { Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "System-level administrator" },
                new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN", Description = "Tenant-level administrator" },
                new ApplicationRole { Name = "Customer", NormalizedName = "CUSTOMER", Description = "End user/customer" }
            };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }
    }
}
