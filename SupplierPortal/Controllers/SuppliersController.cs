using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;
using SupplierPortal.Models.ViewModels;

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

            var accountEmails = await _context.Users
                .Where(u => u.SupplierId != null)
                .ToDictionaryAsync(u => u.SupplierId!.Value, u => u.Email ?? string.Empty);

            var viewModel = new SuppliersIndexViewModel
            {
                Suppliers = suppliers,
                AccountEmailBySupplierId = accountEmails,
                NewAccount = new CreateSupplierAccountViewModel()
            };

            ViewBag.AvailableSuppliersForAccount = new SelectList(
                suppliers.Where(s => !accountEmails.ContainsKey(s.Id)), "Id", "Name");

            return View(viewModel);
        }

        // POST: /Suppliers/CreateAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CreateSupplierAccountViewModel NewAccount)
        {
            var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == NewAccount.SupplierId);
            if (!supplierExists)
            {
                ModelState.AddModelError("NewAccount.SupplierId", "Selected supplier does not exist.");
            }

            var alreadyHasAccount = await _context.Users.AnyAsync(u => u.SupplierId == NewAccount.SupplierId);
            if (alreadyHasAccount)
            {
                ModelState.AddModelError("NewAccount.SupplierId", "This supplier already has an account.");
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = NewAccount.Email,
                    Email = NewAccount.Email,
                    SupplierId = NewAccount.SupplierId
                };

                var result = await _userManager.CreateAsync(user, NewAccount.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, SeedData.SupplierRole);
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    var field = error.Code switch
                    {
                        "DuplicateUserName" => "NewAccount.Email",
                        "InvalidEmail" => "NewAccount.Email",
                        "PasswordTooShort" => "NewAccount.Password",
                        "PasswordRequiresDigit" => "NewAccount.Password",
                        "PasswordRequiresUpper" => "NewAccount.Password",
                        "PasswordRequiresLower" => "NewAccount.Password",
                        "PasswordRequiresNonAlphanumeric" => "NewAccount.Password",
                        _ => string.Empty
                    };

                    ModelState.AddModelError(field, error.Description);
                }
            }

            // Validering misslyckades — bygg om hela Index-vyn med felen synliga
            var suppliers = await _context.Suppliers
                .Include(s => s.AccountManager)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var accountEmails = await _context.Users
                .Where(u => u.SupplierId != null)
                .ToDictionaryAsync(u => u.SupplierId!.Value, u => u.Email ?? string.Empty);

            var viewModel = new SuppliersIndexViewModel
            {
                Suppliers = suppliers,
                AccountEmailBySupplierId = accountEmails,
                NewAccount = NewAccount
            };

            ViewBag.AvailableSuppliersForAccount = new SelectList(
                suppliers.Where(s => !accountEmails.ContainsKey(s.Id)), "Id", "Name", NewAccount.SupplierId);

            return View(nameof(Index), viewModel);
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

            var account = await _context.Users.FirstOrDefaultAsync(u => u.SupplierId == id);
            ViewBag.AccountEmail = account?.Email;

            ViewBag.AccountManagerId = new SelectList(await GetMedsEmployeesAsync(), "Id", "Email", supplier.AccountManagerId);
            return View(supplier);
        }

        // POST: /Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier, string? accountEmail)
        {
            if (id != supplier.Id) return NotFound();

            var account = await _context.Users.FirstOrDefaultAsync(u => u.SupplierId == id);

            // Om leverantören har ett konto och en ny email angavs, validera den separat
            if (account != null && !string.IsNullOrWhiteSpace(accountEmail) && accountEmail != account.Email)
            {
                var existingWithEmail = await _userManager.FindByEmailAsync(accountEmail);
                if (existingWithEmail != null && existingWithEmail.Id != account.Id)
                {
                    ModelState.AddModelError("accountEmail", "That email is already used by another account.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Update(supplier);

                if (account != null && !string.IsNullOrWhiteSpace(accountEmail) && accountEmail != account.Email)
                {
                    account.Email = accountEmail;
                    account.UserName = accountEmail;
                    account.NormalizedEmail = accountEmail.ToUpperInvariant();
                    account.NormalizedUserName = accountEmail.ToUpperInvariant();
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AccountEmail = account?.Email;
            ViewBag.AccountManagerId = new SelectList(await GetMedsEmployeesAsync(), "Id", "Email", supplier.AccountManagerId);
            return View(supplier);
        }

        // POST: /Suppliers/RemoveAccount/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAccount(int supplierId)
        {
            var account = await _context.Users.FirstOrDefaultAsync(u => u.SupplierId == supplierId);
            if (account != null)
            {
                await _userManager.DeleteAsync(account);
            }

            return RedirectToAction(nameof(Edit), new { id = supplierId });
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