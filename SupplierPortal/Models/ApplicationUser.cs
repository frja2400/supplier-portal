using Microsoft.AspNetCore.Identity;

namespace SupplierPortal.Models
{
    public class ApplicationUser : IdentityUser
    {
        // "has login": om detta är ett leverantörskonto, pekar den här på vilken leverantör kontot tillhör. Null för MEDS-medarbetare.
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        // "manages": om detta är ett MEDS-admin-konto, kan användaren vara ansvarig account manager för flera leverantörer.
        public ICollection<Supplier> ManagedSuppliers { get; set; } = new List<Supplier>();
    }
}