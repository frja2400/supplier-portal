using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;

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
        public async Task<IActionResult> Index()
        {
            var activations = await _context.Activations
                .Include(a => a.Supplier)
                .OrderByDescending(a => a.Year)
                .ThenBy(a => a.Period)
                .ToListAsync();

            return View(activations);
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
    }
}