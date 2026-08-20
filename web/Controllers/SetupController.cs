using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Entities;
using web.ViewModels;
using web.Constants;

namespace web.Controllers
{
    /// <summary>
    /// Controller for initial setup tasks
    /// </summary>
    [AllowAnonymous]
    public class SetupController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SetupController> _logger;

        public SetupController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<SetupController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Display first user setup form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FirstUser()
        {
            // If users exist, redirect to home
            if (await _context.Users.AnyAsync())
            {
                return RedirectToAction("Index", "Feed");
            }

            return View();
        }

        /// <summary>
        /// Create first user with Developer role
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FirstUser(FirstUserViewModel model)
        {
            // If users exist, redirect to home
            if (await _context.Users.AnyAsync())
            {
                return RedirectToAction("Index", "Feed");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName,
                EmailConfirmed = true, // Auto-confirm first user
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign Developer role
                var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Developer);

                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("First user created successfully with Developer role: {Email}", user.Email);

                    // Sign in the user
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return RedirectToAction("Index", "Feed");
                }
                else
                {
                    _logger.LogError("Failed to assign Developer role to first user: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    ModelState.AddModelError(string.Empty, "Bruger oprettet, men kunne ikke tildele rolle.");
                }
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}
