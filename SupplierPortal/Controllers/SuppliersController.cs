using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;

namespace SupplierPortal.Controllers
{
    [Authorize(Roles = SeedData.MedsEmployeeRole)]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SuppliersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Suppliers
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.AccountManager)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(suppliers);
        }

        // GET: /Suppliers/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AccountManagerId = new SelectList(await GetMedsEmployeesAsync(), "Id", "Email");
            return View();
        }

        // POST: /Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(supplier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AccountManagerId = new SelectList(await GetMedsEmployeesAsync(), "Id", "Email", supplier.AccountManagerId);
            return View(supplier);
        }

        // GET: /Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            ViewBag.AccountManagerId = new SelectList(await GetMedsEmployeesAsync(), "Id", "Email", supplier.AccountManagerId);
            return View(supplier);
        }

        // POST: /Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(supplier);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AccountManagerId = new SelectList(await GetMedsEmployeesAsync(), "Id", "Email", supplier.AccountManagerId);
            return View(supplier);
        }

        // GET: /Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers
                .Include(s => s.AccountManager)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null) return NotFound();

            return View(supplier);
        }

        // POST: /Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<ApplicationUser>> GetMedsEmployeesAsync()
        {
            return (await _userManager.GetUsersInRoleAsync(SeedData.MedsEmployeeRole)).ToList();
        }
    }
}