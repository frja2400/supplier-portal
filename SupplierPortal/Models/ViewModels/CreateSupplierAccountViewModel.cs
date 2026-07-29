using System.ComponentModel.DataAnnotations;

namespace SupplierPortal.Models.ViewModels
{
    public class CreateSupplierAccountViewModel
    {
        [Required]
        public int SupplierId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}