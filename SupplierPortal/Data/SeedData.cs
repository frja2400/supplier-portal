using Microsoft.AspNetCore.Identity;
using SupplierPortal.Models;

namespace SupplierPortal.Data
{
    public static class SeedData
    {
        public const string MedsEmployeeRole = "MedsEmployee";
        public const string SupplierRole = "Supplier";

        public static async Task EnsureRolesCreatedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { MedsEmployeeRole, SupplierRole };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}