using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SupplierPortal.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? LookerStudioUrl { get; set; }

        public ICollection<Activation> Activations { get; set; } = new List<Activation>();

        // Ansvarig MEDS-admin för denna leverantör (kan vara ospecificerad)
        public string? AccountManagerId { get; set; }
        
        [ValidateNever]
        public ApplicationUser? AccountManager { get; set; }
    }
}