using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SupplierPortal.Models
{
    public class Activation
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Product { get; set; } = string.Empty;

        public int Impressions { get; set; }
        public int Clicks { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Revenue { get; set; }

        [Required]
        [MaxLength(20)]
        public string Period { get; set; } = string.Empty;

        public int Year { get; set; }

        public int SupplierId { get; set; }

        [ValidateNever]
        public Supplier Supplier { get; set; } = null!;
    }
}