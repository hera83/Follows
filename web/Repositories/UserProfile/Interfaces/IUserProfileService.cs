using web.Repositories.UserProfile.Dtos;

namespace web.Repositories.UserProfile.Interfaces
{
    public interface IUserProfileService
    {
        Task<UpdateProfileResponseDto> UpdateProfileAsync(UpdateProfileRequestDto dto, CancellationToken ct = default);
        Task<bool> SaveAvatarAsync(string userId, Stream imageStream, string contentType, string originalFileName, CancellationToken ct = default);
        Task<bool> DeleteAvatarAsync(string userId, CancellationToken ct = default);
        Task<(byte[] Data, string ContentType)?> GetAvatarAsync(string userId, CancellationToken ct = default);
    }
}
