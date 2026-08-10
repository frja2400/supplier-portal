using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;
using ClosedXML.Excel;

namespace SupplierPortal.Controllers
{
    [Authorize(Roles = "MedsEmployee")]
    public class ActivationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Activations
        public async Task<IActionResult> Index(List<int>? supplierId, List<string>? period, string? sortOrder)
        {
            var selectedSupplierIds = supplierId ?? new List<int>();
            var selectedPeriods = period ?? new List<string>();

            var query = BuildFilteredQuery(selectedSupplierIds, selectedPeriods);

            // Revenue är decimal så SQLite kan inte ORDER BY det i databasen. Vi hämtar datan ofiltrerad på sortering här, och sorterar i minnet nedan
            var isRevenueSort = sortOrder is "revenue_asc" or "revenue_desc";

            if (!isRevenueSort)
            {
                query = sortOrder switch
                {
                    "year_asc" => query.OrderBy(a => a.Year).ThenBy(a => a.Period),
                    "year_desc" => query.OrderByDescending(a => a.Year).ThenByDescending(a => a.Period),
                    "product_asc" => query.OrderBy(a => a.Product),
                    "product_desc" => query.OrderByDescending(a => a.Product),
                    "supplier_asc" => query.OrderBy(a => a.Supplier.Name),
                    "supplier_desc" => query.OrderByDescending(a => a.Supplier.Name),
                    "impressions_asc" => query.OrderBy(a => a.Impressions),
                    "impressions_desc" => query.OrderByDescending(a => a.Impressions),
                    "clicks_asc" => query.OrderBy(a => a.Clicks),
                    "clicks_desc" => query.OrderByDescending(a => a.Clicks),
                    "period_asc" => query.OrderBy(a => a.Period),
                    "period_desc" => query.OrderByDescending(a => a.Period),
                    _ => query.OrderByDescending(a => a.Year).ThenByDescending(a => a.Period)
                };
            }

            var activations = await query.ToListAsync();

            // Revenue-sortering sker här, i minnet (LINQ to Objects), efter hämtning
            if (isRevenueSort)
            {
                activations = sortOrder == "revenue_asc"
                    ? activations.OrderBy(a => a.Revenue).ToList()
                    : activations.OrderByDescending(a => a.Revenue).ToList();
            }

            var allSuppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
            var allPeriods = await _context.Activations.Select(a => a.Period).Distinct().OrderBy(p => p).ToListAsync();

            ViewBag.SupplierFilter = new SelectList(
                allSuppliers.Where(s => !selectedSupplierIds.Contains(s.Id)), "Id", "Name");

            ViewBag.PeriodFilter = new SelectList(
                allPeriods.Where(p => !selectedPeriods.Contains(p)));

            ViewBag.CurrentSort = sortOrder;
            ViewBag.SelectedSuppliers = allSuppliers.Where(s => selectedSupplierIds.Contains(s.Id)).ToList();
            ViewBag.SelectedPeriods = selectedPeriods;

            return View(activations);
        }

        private IQueryable<Activation> BuildFilteredQuery(List<int> selectedSupplierIds, List<string> selectedPeriods)
        {
            var query = _context.Activations
                .Include(a => a.Supplier)
                .AsQueryable();

            if (selectedSupplierIds.Any())
            {
                query = query.Where(a => selectedSupplierIds.Contains(a.SupplierId));
            }

            if (selectedPeriods.Any())
            {
                query = query.Where(a => selectedPeriods.Contains(a.Period));
            }

            return query;
        }

        // GET: /Activations/Create
        public IActionResult Create()
        {
            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name");
            return View();
        }

        // POST: /Activations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Activation activation)
        {
            ReplaceInvalidNumberMessages();

            if (ModelState.IsValid)
            {
                _context.Add(activation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", activation.SupplierId);
            return View(activation);
        }

        // GET: /Activations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var activation = await _context.Activations.FindAsync(id);
            if (activation == null) return NotFound();

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", activation.SupplierId);
            return View(activation);
        }

        // POST: /Activations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Activation activation)
        {
            if (id != activation.Id) return NotFound();

            ReplaceInvalidNumberMessages();

            if (ModelState.IsValid)
            {
                _context.Update(activation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SupplierId = new SelectList(_context.Suppliers, "Id", "Name", activation.SupplierId);
            return View(activation);
        }

        // GET: /Activations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var activation = await _context.Activations
                .Include(a => a.Supplier)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activation == null) return NotFound();

            return View(activation);
        }

        // POST: /Activations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var activation = await _context.Activations.FindAsync(id);
            if (activation != null)
            {
                _context.Activations.Remove(activation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Activations/Export
        public async Task<IActionResult> Export(List<int>? supplierId, List<string>? period)
        {
            var selectedSupplierIds = supplierId ?? new List<int>();
            var selectedPeriods = period ?? new List<string>();

            var activations = await BuildFilteredQuery(selectedSupplierIds, selectedPeriods)
                .OrderByDescending(a => a.Year)
                .ThenByDescending(a => a.Period)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Activations");

            string[] headers = { "Supplier", "Product", "Impressions", "Clicks", "Revenue (SEK)", "Period", "Year" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var a in activations)
            {
                worksheet.Cell(row, 1).Value = a.Supplier.Name;
                worksheet.Cell(row, 2).Value = a.Product;
                worksheet.Cell(row, 3).Value = a.Impressions;
                worksheet.Cell(row, 4).Value = a.Clicks;
                worksheet.Cell(row, 5).Value = a.Revenue;
                worksheet.Cell(row, 6).Value = a.Period;
                worksheet.Cell(row, 7).Value = a.Year;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"activations_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private void ReplaceInvalidNumberMessages()
        {
            var numericFields = new Dictionary<string, string>
    {
        { "SupplierId", "Please select a supplier." },
        { "Impressions", "Please enter a valid number for Impressions." },
        { "Clicks", "Please enter a valid number for Clicks." },
        { "Revenue", "Please enter a valid number for Revenue." },
        { "Year", "Please enter a valid number for Year." }
    };

            foreach (var (field, message) in numericFields)
            {
                if (ModelState.TryGetValue(field, out var entry) && entry.Errors.Count > 0)
                {
                    entry.Errors.Clear();
                    entry.Errors.Add(message);
                }
            }
        }
    }
}