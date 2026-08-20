using System.Text.Json.Serialization;

namespace web.Services.Ollama.Dto;

public class OllamaLogProbDto
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("logprob")]
    public double LogProb { get; set; }

    [JsonPropertyName("top_logprobs")]
    public List<OllamaTopLogProbDto>? TopLogProbs { get; set; }
}
