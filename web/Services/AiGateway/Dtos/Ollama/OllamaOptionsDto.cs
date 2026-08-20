using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Ollama;

public class OllamaOptionsDto
{
    [JsonPropertyName("numKeep")]
    public int? NumKeep { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("numPredict")]
    public int? NumPredict { get; set; }

    [JsonPropertyName("topK")]
    public int? TopK { get; set; }

    [JsonPropertyName("topP")]
    public double? TopP { get; set; }

    [JsonPropertyName("minP")]
    public double? MinP { get; set; }

    [JsonPropertyName("typicalP")]
    public double? TypicalP { get; set; }

    [JsonPropertyName("repeatLastN")]
    public int? RepeatLastN { get; set; }

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("repeatPenalty")]
    public double? RepeatPenalty { get; set; }

    [JsonPropertyName("presencePenalty")]
    public double? PresencePenalty { get; set; }

    [JsonPropertyName("frequencyPenalty")]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("mirostat")]
    public int? Mirostat { get; set; }

    [JsonPropertyName("mirostatTau")]
    public double? MirostatTau { get; set; }

    [JsonPropertyName("mirostatEta")]
    public double? MirostatEta { get; set; }

    [JsonPropertyName("penalizeNewline")]
    public bool? PenalizeNewline { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("numa")]
    public bool? Numa { get; set; }

    [JsonPropertyName("numCtx")]
    public int? NumCtx { get; set; }

    [JsonPropertyName("numBatch")]
    public int? NumBatch { get; set; }

    [JsonPropertyName("numGpu")]
    public int? NumGpu { get; set; }

    [JsonPropertyName("mainGpu")]
    public int? MainGpu { get; set; }

    [JsonPropertyName("lowVram")]
    public bool? LowVram { get; set; }

    [JsonPropertyName("vocabOnly")]
    public bool? VocabOnly { get; set; }

    [JsonPropertyName("useMmap")]
    public bool? UseMmap { get; set; }

    [JsonPropertyName("useMlock")]
    public bool? UseMlock { get; set; }

    [JsonPropertyName("numThread")]
    public int? NumThread { get; set; }
}
