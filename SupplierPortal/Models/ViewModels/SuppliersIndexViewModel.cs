namespace SupplierPortal.Models.ViewModels
{
    public class SuppliersIndexViewModel
    {
        public List<Supplier> Suppliers { get; set; } = new();
        public Dictionary<int, string> AccountEmailBySupplierId { get; set; } = new();
        public CreateSupplierAccountViewModel NewAccount { get; set; } = new();
    }
}