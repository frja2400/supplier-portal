using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplierPortal.Data;
using SupplierPortal.Models;

namespace SupplierPortal.Controllers
{
    [ApiController]
    [Route("api/demo")]
    public class DemoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public DemoController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        [HttpPost("reset")]
        public async Task<IActionResult> Reset([FromHeader(Name = "X-Reset-Key")] string? key)
        {
            var expectedKey = _config["Demo:ResetKey"];
            if (string.IsNullOrEmpty(expectedKey) || key != expectedKey)
            {
                return Unauthorized();
            }

            _context.Activations.RemoveRange(_context.Activations);
            _context.Suppliers.RemoveRange(_context.Suppliers);
            await _context.SaveChangesAsync();

            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                await _userManager.DeleteAsync(user);
            }

            await SeedData.SeedDemoDataAsync(_context, _userManager);

            return Ok(new { message = "Demo data reset." });
        }
    }
}