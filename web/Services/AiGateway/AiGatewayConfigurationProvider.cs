using web.Services.AiGateway.Interfaces;

namespace web.Services.AiGateway;

public class AiGatewayConfigurationProvider : IAiGatewayConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public AiGatewayConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<AiGatewaySettings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var settings = new AiGatewaySettings
        {
            BaseUrl = _configuration["AiGateway:BaseUrl"] ?? "http://localhost:5000",
            ApiKey = _configuration["AiGateway:ApiKey"],
            DefaultChatModel = _configuration["AiGateway:DefaultChatModel"],
            DefaultLanguage = _configuration["AiGateway:DefaultLanguage"],
            DefaultTtsModel = _configuration["AiGateway:DefaultTtsModel"],
            DefaultTtsVoice = _configuration["AiGateway:DefaultTtsVoice"],
            DefaultSttModel = _configuration["AiGateway:DefaultSttModel"],
            RequestTimeoutSeconds = int.TryParse(_configuration["AiGateway:RequestTimeoutSeconds"], out var timeout) && timeout > 0 ? timeout : 300,
        };

        return Task.FromResult(settings);
    }
}
