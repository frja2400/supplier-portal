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

        // Skapar demo-data för den publika portfoliolänken. Anropas via DemoController.Reset,
        // inte vid vanlig applikationsuppstart.
        public static async Task SeedDemoDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var nordicGlow = new Supplier { Name = "Nordic Glow AB" };
            var lumina = new Supplier { Name = "Lumina Skincare" };
            context.Suppliers.AddRange(nordicGlow, lumina);
            await context.SaveChangesAsync();

            context.Activations.AddRange(
                new Activation { Supplier = nordicGlow, Product = "Glow Renewal Night Serum", Impressions = 18400, Clicks = 320, Revenue = 24500, Period = "V10-V12", Year = 2026 },
                new Activation { Supplier = lumina, Product = "Lumina Daily Hydration Cream", Impressions = 12750, Clicks = 260, Revenue = 16200, Period = "V10-V12", Year = 2026 }
            );
            await context.SaveChangesAsync();

            var admin = new ApplicationUser { UserName = "demo-admin@example.com", Email = "demo-admin@example.com", EmailConfirmed = true };
            await userManager.CreateAsync(admin, "DemoPass123!");
            await userManager.AddToRoleAsync(admin, MedsEmployeeRole);

            var supplierUser = new ApplicationUser { UserName = "demo-supplier@example.com", Email = "demo-supplier@example.com", SupplierId = nordicGlow.Id, EmailConfirmed = true };
            await userManager.CreateAsync(supplierUser, "DemoPass123!");
            await userManager.AddToRoleAsync(supplierUser, SupplierRole);
        }
    }
}