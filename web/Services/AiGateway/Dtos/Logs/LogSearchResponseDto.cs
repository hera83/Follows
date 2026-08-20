using System.Text.Json.Serialization;

namespace web.Services.AiGateway.Dtos.Logs;

public class LogSearchResponseDto
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("entries")]
    public List<LogEntryResponseDto>? Entries { get; set; }
}
