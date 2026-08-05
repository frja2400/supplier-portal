namespace SupplierPortal.Models
{
    public class ImportedActivationRow
    {
        public int RowNumber { get; set; }

        public string SupplierName { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string ImpressionsRaw { get; set; } = string.Empty;
        public string ClicksRaw { get; set; } = string.Empty;
        public string RevenueRaw { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string YearRaw { get; set; } = string.Empty;

        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();

        public bool SupplierExists { get; set; }
    }
}