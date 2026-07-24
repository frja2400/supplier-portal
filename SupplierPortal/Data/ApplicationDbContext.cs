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
            base.OnModelCreating(builder); // VIKTIGT: måste anropas för att Identity-tabellerna ska skapas korrekt

            // Relation 1: "has login" - en Supplier har (max) en inloggning (1:1)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Supplier)
                .WithMany() // Supplier behöver ingen lista av "users med denna login" - bara en räcker
                .HasForeignKey(u => u.SupplierId)
                .OnDelete(DeleteBehavior.SetNull); // Om Supplier tas bort: sätt kontots SupplierId till null (istället för att radera kontot)

            // Relation 2: "manages" - en MEDS-admin kan hantera flera Suppliers (1:N)
            builder.Entity<Supplier>()
                .HasOne(s => s.AccountManager)
                .WithMany(u => u.ManagedSuppliers)
                .HasForeignKey(s => s.AccountManagerId)
                .OnDelete(DeleteBehavior.SetNull); // Om admin-kontot tas bort: leverantören blir "otilldelad", inte raderad

            // Relation 3: Supplier -> Activations (1:N) - enkel, EF Core klarar denna via konvention,
            // men vi skriver den explicit ändå för tydlighet och konsekvent stil
            builder.Entity<Activation>()
                .HasOne(a => a.Supplier)
                .WithMany(s => s.Activations)
                .HasForeignKey(a => a.SupplierId)
                .OnDelete(DeleteBehavior.Cascade); // Om Supplier tas bort: ta bort dess aktiveringar också
        }
    }
}