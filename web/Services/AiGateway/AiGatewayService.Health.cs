using web.Services.AiGateway.Dtos.Health;

namespace web.Services.AiGateway;

public partial class AiGatewayService
{
    public async Task<HealthResponseDto> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await GetAsync<HealthResponseDto>(client, "Health/Check", cancellationToken);
    }
}
