using System.Text.Json;
using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaRuntimeOptionsDto
{
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public double? TopP { get; set; }

    [JsonPropertyName("top_k")]
    public int? TopK { get; set; }

    [JsonPropertyName("repeat_penalty")]
    public double? RepeatPenalty { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; }

    [JsonPropertyName("num_ctx")]
    public int? NumCtx { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("num_gpu")]
    public int? NumGpu { get; set; }

    [JsonPropertyName("num_thread")]
    public int? NumThread { get; set; }

    [JsonPropertyName("mirostat")]
    public int? Mirostat { get; set; }

    [JsonPropertyName("mirostat_eta")]
    public double? MirostatEta { get; set; }

    [JsonPropertyName("mirostat_tau")]
    public double? MirostatTau { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraOptions { get; set; }
}
