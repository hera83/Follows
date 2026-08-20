using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaEmbedRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public JsonElement Input { get; set; }

    [JsonPropertyName("truncate")]
    public bool? Truncate { get; set; }

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; }

    [JsonPropertyName("keep_alive")]
    public string? KeepAlive { get; set; }

    [JsonPropertyName("options")]
    public OllamaRuntimeOptionsDto? Options { get; set; }

    public static OllamaEmbedRequest FromSingleInput(string model, string input)
    {
        return new OllamaEmbedRequest
        {
            Model = model,
            Input = JsonSerializer.SerializeToElement(input)
        };
    }

    public static OllamaEmbedRequest FromMultipleInput(string model, IEnumerable<string> input)
    {
        return new OllamaEmbedRequest
        {
            Model = model,
            Input = JsonSerializer.SerializeToElement(input)
        };
    }
}
