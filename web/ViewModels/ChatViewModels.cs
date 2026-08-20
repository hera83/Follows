using System.ComponentModel.DataAnnotations;

namespace web.ViewModels
{
    public class ChatWidgetViewModel
    {
        public string Id { get; set; } = $"chat-{Guid.NewGuid():N}";
        public string? Model { get; set; }
        public string Title { get; set; } = "Chat";
        public string Action { get; set; } = "StreamMessage";
        public string Controller { get; set; } = "Chat";
        public bool EnableTts { get; set; }
        public bool EnableStt { get; set; }
    }

    public class ChatSendMessageViewModel
    {
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? Model { get; set; }
        public string? HistoryJson { get; set; }
    }

    public class ChatHistoryMessageViewModel
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ChatSynthesizeSpeechViewModel
    {
        [Required]
        [StringLength(8000, ErrorMessage = "Tekst må ikke være længere end 8000 tegn")]
        public string Text { get; set; } = string.Empty;
    }

    public class ChatTranscribeSpeechViewModel
    {
        [Required(ErrorMessage = "Ingen lydoptagelse modtaget")]
        public IFormFile? Audio { get; set; }
    }
}
