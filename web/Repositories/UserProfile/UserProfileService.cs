using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.UserProfile.Dtos;
using web.Repositories.UserProfile.Interfaces;

namespace web.Repositories.UserProfile
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<UserProfileService> logger)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
        }

        public async Task<UpdateProfileResponseDto> UpdateProfileAsync(UpdateProfileRequestDto dto, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null)
                return new UpdateProfileResponseDto { Success = false, ErrorMessage = "Bruger ikke fundet." };

            user.DisplayName = dto.DisplayName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber;
            user.PreferredLanguage = AppLanguages.Normalize(dto.PreferredLanguage);
            user.UpdatedAtUtc = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new UpdateProfileResponseDto
                {
                    Success = false,
                    ErrorMessage = string.Join(" ", updateResult.Errors.Select(e => e.Description))
                };
            }

            if (!string.IsNullOrWhiteSpace(dto.NewUserName))
            {
                var existing = await _userManager.FindByNameAsync(dto.NewUserName);
                if (existing is not null && existing.Id != user.Id)
                {
                    return new UpdateProfileResponseDto
                    {
                        Success = false,
                        ErrorMessage = $"Brugernavnet '{dto.NewUserName}' er allerede taget."
                    };
                }

                var usernameResult = await _userManager.SetUserNameAsync(user, dto.NewUserName);
                if (!usernameResult.Succeeded)
                {
                    return new UpdateProfileResponseDto
                    {
                        Success = false,
                        ErrorMessage = string.Join(" ", usernameResult.Errors.Select(e => e.Description))
                    };
                }
            }

            if (!string.IsNullOrEmpty(dto.NewPassword))
            {
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    return new UpdateProfileResponseDto
                    {
                        Success = false,
                        ErrorMessage = string.Join(" ", removeResult.Errors.Select(e => e.Description))
                    };
                }

                var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
                if (!addResult.Succeeded)
                {
                    return new UpdateProfileResponseDto
                    {
                        Success = false,
                        ErrorMessage = string.Join(" ", addResult.Errors.Select(e => e.Description))
                    };
                }
            }

            _logger.LogInformation("User {UserId} updated profile", dto.UserId);
            return new UpdateProfileResponseDto { Success = true };
        }

        public async Task<bool> SaveAvatarAsync(string userId, Stream imageStream, string contentType, string originalFileName, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return false;

            var filesPath = _config["AppSettings:FilesPath"] ?? "App_files";
            var avatarDir = Path.Combine(_env.ContentRootPath, filesPath, FileCategories.Avatars);
            Directory.CreateDirectory(avatarDir);

            var ext = contentType.Contains("png") ? ".png" : ".jpg";
            var storedFileName = $"{Guid.NewGuid()}{ext}";
            var storedRelativePath = Path.Combine(filesPath, FileCategories.Avatars, storedFileName);
            var fullPath = Path.Combine(_env.ContentRootPath, storedRelativePath);

            // Remove old avatar before saving new one
            await DeleteAvatarAsync(userId, ct);

            await using (var fileStream = File.Create(fullPath))
            {
                await imageStream.CopyToAsync(fileStream, ct);
            }

            var fileInfo = new FileInfo(fullPath);

            var metadata = new FileMetadata
            {
                OriginalFileName = originalFileName,
                StoredFileName = storedFileName,
                StoredPath = storedRelativePath,
                ContentType = contentType,
                FileSizeBytes = fileInfo.Length,
                OwnerId = userId,
                Category = FileCategories.Avatars,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.FileMetadata.Add(metadata);
            await _context.SaveChangesAsync(ct);

            user.ProfilePictureId = metadata.Id;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} uploaded avatar: {FileName}", userId, storedFileName);
            return true;
        }

        public async Task<bool> DeleteAvatarAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.ProfilePictureId is null) return false;

            var metadata = await _context.FileMetadata.FindAsync([user.ProfilePictureId], ct);
            if (metadata is not null && !metadata.IsDeleted)
            {
                var fullPath = Path.Combine(_env.ContentRootPath, metadata.StoredPath);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);

                metadata.IsDeleted = true;
                metadata.DeletedAtUtc = DateTime.UtcNow;
                metadata.UpdatedAtUtc = DateTime.UtcNow;
                _context.FileMetadata.Update(metadata);
            }

            user.ProfilePictureId = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} deleted avatar", userId);
            return true;
        }

        public async Task<(byte[] Data, string ContentType)?> GetAvatarAsync(string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || user.ProfilePictureId is null) return null;

            var metadata = await _context.FileMetadata
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == user.ProfilePictureId && !f.IsDeleted, ct);

            if (metadata is null) return null;

            var fullPath = Path.Combine(_env.ContentRootPath, metadata.StoredPath);
            if (!File.Exists(fullPath)) return null;

            var data = await File.ReadAllBytesAsync(fullPath, ct);
            return (data, metadata.ContentType);
        }
    }
}
