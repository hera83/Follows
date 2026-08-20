using System.Globalization;
using System.Web;
using web.Services.AiGateway.Dtos.Logs;

namespace web.Services.AiGateway;

public partial class AiGatewayService
{
    public async Task<LogSearchResponseDto> SearchLogsAsync(LogSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);

        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(request.Level))
        {
            query["Level"] = request.Level;
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query["Search"] = request.Search;
        }
        if (request.From.HasValue)
        {
            query["From"] = request.From.Value.ToString("o", CultureInfo.InvariantCulture);
        }
        if (request.To.HasValue)
        {
            query["To"] = request.To.Value.ToString("o", CultureInfo.InvariantCulture);
        }
        if (request.Page.HasValue)
        {
            query["Page"] = request.Page.Value.ToString(CultureInfo.InvariantCulture);
        }
        if (request.PageSize.HasValue)
        {
            query["PageSize"] = request.PageSize.Value.ToString(CultureInfo.InvariantCulture);
        }

        var endpoint = "Logs/Search";
        var queryString = query.ToString();
        if (!string.IsNullOrEmpty(queryString))
        {
            endpoint += "?" + queryString;
        }

        return await GetAsync<LogSearchResponseDto>(client, endpoint, cancellationToken);
    }
}
