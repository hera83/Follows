using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using web.Services.AiGateway.Dtos;
using web.Services.AiGateway.Dtos.Errors;
using web.Services.AiGateway.Interfaces;

namespace web.Services.AiGateway;

public partial class AiGatewayService : IAiGatewayService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IAiGatewayConfigurationProvider _configurationProvider;
    private readonly AiGatewayHttpClientFactory _httpClientFactory;

    public AiGatewayService(
        IAiGatewayConfigurationProvider configurationProvider,
        AiGatewayHttpClientFactory httpClientFactory)
    {
        _configurationProvider = configurationProvider;
        _httpClientFactory = httpClientFactory;
    }

    private async Task<HttpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var config = await _configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        return _httpClientFactory.Create(config.BaseUrl, config.ApiKey, config.RequestTimeoutSeconds);
    }

    private static async Task<TResponse> GetAsync<TResponse>(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new AiGatewayException(HttpStatusCode.InternalServerError, "Kunne ikke parse AiGateway response.");
    }

    private static async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient client, string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(client, HttpMethod.Post, endpoint, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new AiGatewayException(HttpStatusCode.InternalServerError, "Kunne ikke parse AiGateway response.");
    }

    private static async Task<TResponse> PutAsync<TRequest, TResponse>(HttpClient client, string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(client, HttpMethod.Put, endpoint, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new AiGatewayException(HttpStatusCode.InternalServerError, "Kunne ikke parse AiGateway response.");
    }

    private static async Task<TResponse> PostAsync<TResponse>(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsync(endpoint, null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions)
               ?? throw new AiGatewayException(HttpStatusCode.InternalServerError, "Kunne ikke parse AiGateway response.");
    }

    private static async Task DeleteAsync(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await client.DeleteAsync(endpoint, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task DeleteWithBodyAsync<TRequest>(HttpClient client, string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(client, HttpMethod.Delete, endpoint, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task PostNoContentAsync<TRequest>(HttpClient client, string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendJsonAsync(client, HttpMethod.Post, endpoint, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendJsonAsync<TRequest>(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var message = new HttpRequestMessage(method, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        return await client.SendAsync(message, completionOption, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendMultipartAsync(
        HttpClient client,
        HttpMethod method,
        string endpoint,
        MultipartFormDataContent content,
        CancellationToken cancellationToken,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
    {
        var message = new HttpRequestMessage(method, endpoint) { Content = content };
        return await client.SendAsync(message, completionOption, cancellationToken);
    }

    private static void AddFilePart(MultipartFormDataContent content, string fieldName, Stream fileContent, string fileName, string? contentType)
    {
        var streamContent = new StreamContent(fileContent);
        // Browseres Blob.type (fx MediaRecorders "audio/webm;codecs=opus") har ofte parametre,
        // men AiGateway kaster en uhåndteret 500 hvis Content-Type på fil-delen har parametre -
        // send derfor kun det rene "type/subtype" videre.
        streamContent.Headers.ContentType = string.IsNullOrWhiteSpace(contentType)
            ? new MediaTypeHeaderValue("application/octet-stream")
            : new MediaTypeHeaderValue(MediaTypeHeaderValue.Parse(contentType).MediaType!);
        content.Add(streamContent, fieldName, fileName);
    }

    private static void AddFormValue(MultipartFormDataContent content, string fieldName, string? value)
    {
        if (value is not null)
        {
            content.Add(new StringContent(value), fieldName);
        }
    }

    private static async Task<AiGatewayFileResultDto> GetFileAsync(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ToFileResultAsync(response, cancellationToken);
    }

    private static async Task<AiGatewayFileResultDto> ToFileResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new AiGatewayFileResultDto
        {
            Content = stream,
            ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
            FileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName
        };
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        string message = $"AiGateway request fejlede med status {(int)response.StatusCode}.";
        string? traceId = null;
        IReadOnlyDictionary<string, string[]>? errors = null;

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var error = JsonSerializer.Deserialize<ErrorResponseDto>(raw, JsonOptions);
                if (error is not null)
                {
                    message = !string.IsNullOrWhiteSpace(error.Detail) ? error.Detail : error.Title ?? raw;
                    traceId = error.TraceId;
                    errors = error.Errors;
                }
                else
                {
                    message = raw;
                }
            }
            catch
            {
                message = raw;
            }
        }

        throw new AiGatewayException(response.StatusCode, message, traceId, errors);
    }
}
