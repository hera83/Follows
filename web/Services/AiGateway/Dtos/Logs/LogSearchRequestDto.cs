using System.ComponentModel.DataAnnotations;

namespace web.Services.AiGateway.Dtos.Logs;

public class LogSearchRequestDto
{
    public string? Level { get; set; }
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [Range(1, int.MaxValue)]
    public int? Page { get; set; }

    [Range(1, 500)]
    public int? PageSize { get; set; }
}
