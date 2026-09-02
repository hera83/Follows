namespace web.Services.VideoTranscoding.Dtos
{
    public class TranscodeVideoResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public long OutputFileSizeBytes { get; set; }
    }
}
