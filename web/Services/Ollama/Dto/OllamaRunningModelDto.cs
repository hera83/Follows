using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaRunningModelDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("digest")]
    public string Digest { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public OllamaModelDetailsDto? Details { get; set; }

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [JsonPropertyName("size_vram")]
    public long? SizeVram { get; set; }

    [JsonPropertyName("context_length")]
    public int? ContextLength { get; set; }
}
