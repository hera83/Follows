using System.Net;

namespace web.Services.AiGateway;

public class AiGatewayException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? TraceId { get; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public AiGatewayException(HttpStatusCode statusCode, string message, string? traceId = null, IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        TraceId = traceId;
        Errors = errors;
    }
}
