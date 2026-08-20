using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.ViewModels;

namespace web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Feed");
            }

            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl,
                AllowRegistration = await IsPublicRegistrationAllowedAsync()
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UsernameOrEmail)
                       ?? await _userManager.FindByEmailAsync(model.UsernameOrEmail);

            if (user is null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Ugyldig login eller deaktiveret bruger.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in: {UserName}", user.UserName);
                if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Feed");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Brugeren er midlertidigt låst. Prøv igen senere.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Ugyldigt loginforsøg.");
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (!await IsPublicRegistrationAllowedAsync())
            {
                TempData["WarningMessage"] = "Offentlig brugertilmelding er deaktiveret.";
                return RedirectToAction(nameof(Login));
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!await IsPublicRegistrationAllowedAsync())
            {
                TempData["WarningMessage"] = "Offentlig brugertilmelding er deaktiveret.";
                return RedirectToAction(nameof(Login));
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
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, AppRoles.User);
                _logger.LogInformation("Public user registered: {Email}", user.Email);
                TempData["SuccessMessage"] = "Bruger oprettet. Du kan nu logge ind.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<bool> IsPublicRegistrationAllowedAsync()
        {
            var setting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == AppSettingKeys.AllowPublicRegistration);

            return bool.TryParse(setting?.Value, out var allowed) && allowed;
        }
    }
}
