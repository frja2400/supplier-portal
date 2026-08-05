using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;
using SupplierPortal.Services;

namespace SupplierPortal.Controllers
{
    [Authorize(Roles = SeedData.MedsEmployeeRole)]
    public class ImportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ActivationImportService _importService;

        public ImportController(ApplicationDbContext context, ActivationImportService importService)
        {
            _context = context;
            _importService = importService;
        }

        // GET: /Import
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Import/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Please choose a file to upload.");
                return View(nameof(Index));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            List<ImportedActivationRow> rows;

            using (var stream = file.OpenReadStream())
            {
                if (extension == ".xlsx")
                {
                    rows = _importService.ParseXlsx(stream);
                }
                else if (extension == ".csv")
                {
                    rows = _importService.ParseCsv(stream);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Please upload a .xlsx or .csv file.");
                    return View(nameof(Index));
                }
            }

            if (!rows.Any())
            {
                ModelState.AddModelError(string.Empty, "No data rows found in the file.");
                return View(nameof(Index));
            }

            // Validera varje rad, och kolla om leverantören redan finns
            var existingSupplierNames = await _context.Suppliers
                .Select(s => s.Name.ToLower())
                .ToListAsync();

            foreach (var row in rows)
            {
                _importService.Validate(row);
                row.SupplierExists = existingSupplierNames.Contains(row.SupplierName.ToLower());
            }

            return View("Preview", rows);
        }

        // POST: /Import/Confirm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(List<ImportedActivationRow> rows)
        {
            var validRows = rows.Where(r => r.IsValid).ToList();

            if (!validRows.Any())
            {
                TempData["ImportError"] = "No valid rows to import.";
                return RedirectToAction(nameof(Index));
            }

            var existingSuppliers = await _context.Suppliers.ToListAsync();
            var suppliersByName = existingSuppliers.ToDictionary(s => s.Name.ToLower());

            int created = 0;
            int suppliersCreated = 0;

            foreach (var row in validRows)
            {
                var key = row.SupplierName.ToLower();

                if (!suppliersByName.TryGetValue(key, out var supplier))
                {
                    supplier = new Supplier { Name = row.SupplierName };
                    _context.Suppliers.Add(supplier);
                    suppliersByName[key] = supplier;
                    suppliersCreated++;
                }

                ActivationImportService.TryParseInt(row.ImpressionsRaw, out var impressions);
                ActivationImportService.TryParseInt(row.ClicksRaw, out var clicks);
                ActivationImportService.TryParseDecimal(row.RevenueRaw, out var revenue);
                ActivationImportService.TryParseInt(row.YearRaw, out var year);

                var activation = new Activation
                {
                    Supplier = supplier,
                    Product = row.Product,
                    Impressions = impressions,
                    Clicks = clicks,
                    Revenue = revenue,
                    Period = row.Period,
                    Year = year
                };

                _context.Activations.Add(activation);
                created++;
            }

            await _context.SaveChangesAsync();

            TempData["ImportSuccess"] = $"Imported {created} activation(s), created {suppliersCreated} new supplier(s).";
            return RedirectToAction("Index", "Activations");
        }
    }
}