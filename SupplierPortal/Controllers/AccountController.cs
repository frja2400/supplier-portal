using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SupplierPortal.Data;
using SupplierPortal.Models;
using SupplierPortal.Models.ViewModels;

namespace SupplierPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, isPersistent: false, lockoutOnFailure: true);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            const string allowedDomain = "@meds.se";

            if (!model.Email.EndsWith(allowedDomain, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Email), $"Only email addresses ending in {allowedDomain} can register an account.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, SeedData.MedsEmployeeRole);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                var (field, message) = MapErrorToField(error);
                ModelState.AddModelError(field, message);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private static (string Field, string Message) MapErrorToField(IdentityError error)
        {
            var field = error.Code switch
            {
                "PasswordTooShort" => nameof(RegisterViewModel.Password),
                "PasswordRequiresDigit" => nameof(RegisterViewModel.Password),
                "PasswordRequiresUpper" => nameof(RegisterViewModel.Password),
                "PasswordRequiresLower" => nameof(RegisterViewModel.Password),
                "PasswordRequiresNonAlphanumeric" => nameof(RegisterViewModel.Password),
                "DuplicateUserName" => nameof(RegisterViewModel.Email),
                _ => string.Empty // unknown error code: falls back to the validation summary
            };

            return (field, error.Description); // Identity's default English text, used as-is
        }
    }
}