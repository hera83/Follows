namespace web.Services.AiGateway;

public class AiGatewaySettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string? ApiKey { get; set; }
    public string? DefaultChatModel { get; set; }
    public string? DefaultLanguage { get; set; }
    public string? DefaultTtsModel { get; set; }
    public string? DefaultTtsVoice { get; set; }
    public string? DefaultSttModel { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 300;
}
