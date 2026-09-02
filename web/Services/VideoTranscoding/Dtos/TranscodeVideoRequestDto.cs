namespace web.Services.VideoTranscoding.Dtos
{
    /// <summary>Re-encode one video file to a broadly browser-compatible H.264/AAC MP4.</summary>
    public class TranscodeVideoRequestDto
    {
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
    }
}
