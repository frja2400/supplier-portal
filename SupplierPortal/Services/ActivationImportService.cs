using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using SupplierPortal.Models;

namespace SupplierPortal.Services
{
    public class ActivationImportService
    {
        // Kolumnordning enligt MEDS mall

        public List<ImportedActivationRow> ParseXlsx(Stream fileStream)
        {
            var rows = new List<ImportedActivationRow>();

            using var workbook = new XLWorkbook(fileStream);

            // Försök hitta arket "Sponsrade produkter", annars ta första arket
            var worksheet = workbook.Worksheets.Contains("Sponsrade produkter")
                ? workbook.Worksheet("Sponsrade produkter")
                : workbook.Worksheet(1);

            var rowNumber = 1;
            foreach (var row in worksheet.RowsUsed().Skip(1)) // hoppa över rubrikraden
            {
                rowNumber++;

                var supplierName = row.Cell(1).GetString().Trim();
                var product = row.Cell(2).GetString().Trim();

                // Hoppa helt över helt tomma rader (t.ex. tomrum i slutet av filen)
                if (string.IsNullOrWhiteSpace(supplierName) && string.IsNullOrWhiteSpace(product))
                {
                    continue;
                }

                rows.Add(new ImportedActivationRow
                {
                    RowNumber = rowNumber,
                    SupplierName = supplierName,
                    Product = product,
                    ImpressionsRaw = row.Cell(3).GetString().Trim(),
                    ClicksRaw = row.Cell(4).GetString().Trim(),
                    RevenueRaw = row.Cell(5).GetString().Trim(),
                    Period = row.Cell(6).GetString().Trim(),
                    YearRaw = row.Cell(7).GetString().Trim()
                });
            }

            return rows;
        }

        public List<ImportedActivationRow> ParseCsv(Stream fileStream)
        {
            var rows = new List<ImportedActivationRow>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null
            };

            using var reader = new StreamReader(fileStream);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader(); // hoppar över rubrikraden

            var rowNumber = 1;
            while (csv.Read())
            {
                rowNumber++;

                var supplierName = csv.GetField(0)?.Trim() ?? string.Empty;
                var product = csv.GetField(1)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(supplierName) && string.IsNullOrWhiteSpace(product))
                {
                    continue;
                }

                rows.Add(new ImportedActivationRow
                {
                    RowNumber = rowNumber,
                    SupplierName = supplierName,
                    Product = product,
                    ImpressionsRaw = csv.GetField(2)?.Trim() ?? string.Empty,
                    ClicksRaw = csv.GetField(3)?.Trim() ?? string.Empty,
                    RevenueRaw = csv.GetField(4)?.Trim() ?? string.Empty,
                    Period = csv.GetField(5)?.Trim() ?? string.Empty,
                    YearRaw = csv.GetField(6)?.Trim() ?? string.Empty
                });
            }

            return rows;
        }

        public void Validate(ImportedActivationRow row)
        {
            row.Errors.Clear();

            if (string.IsNullOrWhiteSpace(row.SupplierName))
                row.Errors.Add("Supplier name is missing.");

            if (string.IsNullOrWhiteSpace(row.Product))
                row.Errors.Add("Product is missing.");

            if (!TryParseInt(row.ImpressionsRaw, out _))
                row.Errors.Add($"Impressions ('{row.ImpressionsRaw}') is not a valid number.");

            if (!TryParseInt(row.ClicksRaw, out _))
                row.Errors.Add($"Clicks ('{row.ClicksRaw}') is not a valid number.");

            if (!TryParseDecimal(row.RevenueRaw, out _))
                row.Errors.Add($"Revenue ('{row.RevenueRaw}') is not a valid number.");

            if (string.IsNullOrWhiteSpace(row.Period))
                row.Errors.Add("Period is missing.");

            if (!TryParseInt(row.YearRaw, out var year) || year < 2000 || year > 2100)
                row.Errors.Add($"Year ('{row.YearRaw}') is not a valid year.");

            row.IsValid = row.Errors.Count == 0;
        }

        public static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseDecimal(string value, out decimal result)
        {
            // Tillåt både punkt och komma som decimaltecken
            var normalized = value.Replace(",", ".");
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
        }
    }
}