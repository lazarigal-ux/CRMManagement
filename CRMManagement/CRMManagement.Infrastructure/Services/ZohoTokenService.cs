using System.Text.Json;
using CRMManagement.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class ZohoTokenService : IZohoTokenService
{
    private const string AccessTokenType = "Zoho:AccessToken";
    private const int SafetyWindowMinutes = 5;

    private readonly ITokenStore _tokens;
    private readonly IZohoConnectionService _connections;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<ZohoTokenService> _logger;

    public ZohoTokenService(
        ITokenStore tokens,
        IZohoConnectionService connections,
        IHttpClientFactory http,
        ILogger<ZohoTokenService> logger)
    {
        _tokens = tokens;
        _connections = connections;
        _http = http;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(Guid connectionId, CancellationToken ct = default)
    {
        var cached = await _tokens.GetAsync(connectionId, AccessTokenType, ct);
        if (cached is not null)
            return cached;

        var (region, clientId, clientSecret, refreshToken) = await _connections.GetCredentialsAsync(ct);

        var (accessToken, expiresIn) = await ExchangeRefreshTokenAsync(region, clientId, clientSecret, refreshToken, ct);

        var expiresAt = DateTime.UtcNow + expiresIn - TimeSpan.FromMinutes(SafetyWindowMinutes);
        await _tokens.SaveAsync(connectionId, AccessTokenType, accessToken, expiresAt, ct);

        return accessToken;
    }

    private async Task<(string AccessToken, TimeSpan ExpiresIn)> ExchangeRefreshTokenAsync(
        string region, string clientId, string clientSecret, string refreshToken, CancellationToken ct)
    {
        var client = _http.CreateClient(ZohoTokenProvider.HttpClientName);
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id",     clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type",    "refresh_token"),
        });

        using var response = await client.PostAsync($"https://accounts.zoho.{region}/oauth/v2/token", form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Zoho token endpoint returned {Status}: {Body}", (int)response.StatusCode, Truncate(body, 1000));
            throw new InvalidOperationException($"Zoho token refresh failed ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errEl))
            throw new InvalidOperationException($"Zoho token exchange error: {errEl.GetString() ?? "unknown"}");

        if (!root.TryGetProperty("access_token", out var accessEl) || accessEl.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException($"Zoho token response missing access_token: {Truncate(body, 500)}");

        var accessToken = accessEl.GetString()!;
        var expiresIn = TimeSpan.FromSeconds(3600);
        if (root.TryGetProperty("expires_in", out var expEl) && expEl.ValueKind == JsonValueKind.Number)
        {
            var raw = expEl.GetInt64();
            expiresIn = raw > 86400 ? TimeSpan.FromMilliseconds(raw) : TimeSpan.FromSeconds(raw);
        }

        return (accessToken, expiresIn);
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s[..max];
}
