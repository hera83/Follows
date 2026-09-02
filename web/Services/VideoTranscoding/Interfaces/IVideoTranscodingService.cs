using web.Services.VideoTranscoding.Dtos;

namespace web.Services.VideoTranscoding.Interfaces
{
    /// <summary>Re-encodes video files to a broadly browser-compatible format via the ffmpeg CLI.</summary>
    public interface IVideoTranscodingService
    {
        Task<TranscodeVideoResponseDto> TranscodeToH264Async(TranscodeVideoRequestDto request, CancellationToken cancellationToken = default);
    }
}
