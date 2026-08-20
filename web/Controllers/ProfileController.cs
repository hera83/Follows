using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.UserProfile.Dtos;
using web.Repositories.UserProfile.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxAvatarBytes = 5 * 1024 * 1024; // 5 MB

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProfileService _profileService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            IUserProfileService profileService,
            ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _profileService = profileService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            var currentUserName = user.UserName != user.Email ? user.UserName : string.Empty;

            var vm = new ProfileViewModel
            {
                UserName = currentUserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                HasAvatar = user.ProfilePictureId.HasValue,
                ThemePreference = user.ThemePreference ?? ThemeMode.System,
                EditForm = new EditProfileViewModel
                {
                    UserName = currentUserName,
                    DisplayName = user.DisplayName,
                    PhoneNumber = user.PhoneNumber,
                    PreferredLanguage = AppLanguages.Normalize(user.PreferredLanguage)
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Profilen kunne ikke opdateres. Kontroller felterne." : errors);
            }

            var result = await _profileService.UpdateProfileAsync(new UpdateProfileRequestDto
            {
                UserId = user.Id,
                DisplayName = model.EditForm.DisplayName,
                PhoneNumber = model.EditForm.PhoneNumber,
                PreferredLanguage = model.EditForm.PreferredLanguage,
                NewUserName = string.IsNullOrWhiteSpace(model.EditForm.UserName) ? null : model.EditForm.UserName,
                NewPassword = model.EditForm.NewPassword
            }, HttpContext.RequestAborted);

            return result.Success
                ? this.ToastSuccessJson("Profil opdateret.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Opdatering mislykkedes.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetTheme(string themePreference)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var validModes = new[] { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System };
            user.ThemePreference = validModes.Contains(themePreference) ? themePreference : ThemeMode.System;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile croppedImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (croppedImage is null || croppedImage.Length == 0)
                return BadRequest(new { error = "Ingen fil modtaget." });

            if (!AllowedImageTypes.Contains(croppedImage.ContentType))
                return BadRequest(new { error = "Filtype er ikke tilladt. Brug JPEG, PNG eller WebP." });

            if (croppedImage.Length > MaxAvatarBytes)
                return BadRequest(new { error = "Filen er for stor (max 5 MB)." });

            await using var stream = croppedImage.OpenReadStream();
            var saved = await _profileService.SaveAvatarAsync(
                user.Id,
                stream,
                croppedImage.ContentType,
                croppedImage.FileName,
                HttpContext.RequestAborted);

            if (!saved)
            {
                _logger.LogWarning("Avatar save failed for user {UserId}", user.Id);
                return StatusCode(500, new { error = "Kunne ikke gemme billede." });
            }

            return Ok(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            await _profileService.DeleteAvatarAsync(user.Id, HttpContext.RequestAborted);
            return this.ToastSuccessJson("Profilbillede fjernet.");
        }

        [HttpGet]
        public async Task<IActionResult> Avatar(string? userId = null)
        {
            var targetUserId = string.IsNullOrEmpty(userId)
                ? _userManager.GetUserId(User)
                : userId;

            if (string.IsNullOrEmpty(targetUserId)) return NotFound();

            var result = await _profileService.GetAvatarAsync(targetUserId, HttpContext.RequestAborted);
            if (result is null) return NotFound();

            return File(result.Value.Data, result.Value.ContentType);
        }
    }
}
