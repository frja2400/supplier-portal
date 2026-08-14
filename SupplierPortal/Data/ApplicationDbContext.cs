using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Models;

namespace SupplierPortal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Activation> Activations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relation 1: "has login" - en Supplier har (max) en inloggning (1:1)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Supplier)
                .WithMany() 
                .HasForeignKey(u => u.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relation 2: "manages" - en MEDS-admin kan hantera flera Suppliers (1:N)
            builder.Entity<Supplier>()
                .HasOne(s => s.AccountManager)
                .WithMany(u => u.ManagedSuppliers)
                .HasForeignKey(s => s.AccountManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relation 3: Supplier -> Activations (1:N) - enkel, EF Core klarar denna via konvention,
            builder.Entity<Activation>()
                .HasOne(a => a.Supplier)
                .WithMany(s => s.Activations)
                .HasForeignKey(a => a.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}