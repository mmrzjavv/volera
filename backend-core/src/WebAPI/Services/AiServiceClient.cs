using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Core.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace WebAPI.Services;

public class AiServiceClientOptions
{
    public const string SectionName = "AiService";
    public string BaseUrl { get; set; } = "";
    public string? ApiKey { get; set; }
}

public class AiServiceClient : IAiServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiServiceClient> _logger;

    public AiServiceClient(HttpClient httpClient, IOptions<AiServiceClientOptions> options, ILogger<AiServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        var baseUrl = options.Value.BaseUrl?.TrimEnd('/') ?? "";
        if (!string.IsNullOrEmpty(baseUrl))
            _httpClient.BaseAddress = new Uri(baseUrl);
        if (!string.IsNullOrEmpty(options.Value.ApiKey))
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", options.Value.ApiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var body = new { text = text ?? "" };
        var response = await _httpClient.PostAsJsonAsync("embed", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI embed failed: {StatusCode} {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"AI service embed failed: {response.StatusCode}");
        }
        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken);
        return result?.Embedding ?? Array.Empty<float>();
    }

    public async Task<string> ChatAsync(string tenantId, string message, string? sessionId, CancellationToken cancellationToken = default)
    {
        var body = new { tenant_id = tenantId, message, session_id = sessionId };
        var response = await _httpClient.PostAsJsonAsync("chat", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI chat failed: {StatusCode} {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"AI service chat failed: {response.StatusCode}");
        }
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken);
        return result?.Answer ?? "";
    }

    private class ChatResponse
    {
        public string Answer { get; set; } = "";
    }

    private class EmbedResponse
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
