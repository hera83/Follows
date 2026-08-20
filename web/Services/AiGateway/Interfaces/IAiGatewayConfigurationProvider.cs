namespace web.Services.AiGateway.Interfaces;

public interface IAiGatewayConfigurationProvider
{
    Task<AiGatewaySettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
}
