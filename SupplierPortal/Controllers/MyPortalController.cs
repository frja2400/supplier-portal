using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;
using ClosedXML.Excel;

namespace SupplierPortal.Controllers
{
    [Authorize(Roles = SeedData.SupplierRole)]
    public class MyPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyPortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /MyPortal
        public async Task<IActionResult> Index(List<string>? period, string? sortOrder)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.SupplierId == null)
            {
                return NotFound("No supplier is linked to this account.");
            }

            var supplier = await _context.Suppliers
                .Include(s => s.AccountManager)
                .FirstOrDefaultAsync(s => s.Id == currentUser.SupplierId);

            if (supplier == null)
            {
                return NotFound();
            }

            var selectedPeriods = period ?? new List<string>();

            var query = _context.Activations
                .Where(a => a.SupplierId == supplier.Id)
                .AsQueryable();

            if (selectedPeriods.Any())
            {
                query = query.Where(a => selectedPeriods.Contains(a.Period));
            }

            var isRevenueSort = sortOrder is "revenue_asc" or "revenue_desc";

            if (!isRevenueSort)
            {
                query = sortOrder switch
                {
                    "year_asc" => query.OrderBy(a => a.Year).ThenBy(a => a.Period),
                    "year_desc" => query.OrderByDescending(a => a.Year).ThenByDescending(a => a.Period),
                    "product_asc" => query.OrderBy(a => a.Product),
                    "product_desc" => query.OrderByDescending(a => a.Product),
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

            if (isRevenueSort)
            {
                activations = sortOrder == "revenue_asc"
                    ? activations.OrderBy(a => a.Revenue).ToList()
                    : activations.OrderByDescending(a => a.Revenue).ToList();
            }

            var allPeriods = await _context.Activations
                .Where(a => a.SupplierId == supplier.Id)
                .Select(a => a.Period)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            ViewBag.Supplier = supplier;
            ViewBag.PeriodFilter = new SelectList(allPeriods.Where(p => !selectedPeriods.Contains(p)));
            ViewBag.SelectedPeriods = selectedPeriods;
            ViewBag.CurrentSort = sortOrder;

            return View(activations);
        }

        // GET: /MyPortal/Export
        public async Task<IActionResult> Export(List<string>? period)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.SupplierId == null)
            {
                return NotFound("No supplier is linked to this account.");
            }

            var supplier = await _context.Suppliers.FindAsync(currentUser.SupplierId);
            if (supplier == null)
            {
                return NotFound();
            }

            var selectedPeriods = period ?? new List<string>();

            var query = _context.Activations
                .Where(a => a.SupplierId == supplier.Id)
                .AsQueryable();

            if (selectedPeriods.Any())
            {
                query = query.Where(a => selectedPeriods.Contains(a.Period));
            }

            var activations = await query
                .OrderByDescending(a => a.Year)
                .ThenByDescending(a => a.Period)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("My activations");

            string[] headers = { "Product", "Impressions", "Clicks", "Revenue (SEK)", "Period", "Year" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            int row = 2;
            foreach (var a in activations)
            {
                worksheet.Cell(row, 1).Value = a.Product;
                worksheet.Cell(row, 2).Value = a.Impressions;
                worksheet.Cell(row, 3).Value = a.Clicks;
                worksheet.Cell(row, 4).Value = a.Revenue;
                worksheet.Cell(row, 5).Value = a.Period;
                worksheet.Cell(row, 6).Value = a.Year;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var safeName = supplier.Name.Replace(" ", "_");
            var fileName = $"{safeName}_activations_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}