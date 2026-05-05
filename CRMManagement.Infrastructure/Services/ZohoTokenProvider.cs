using System.Text.Json;
using CRMManagement.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class ZohoTokenProvider : IZohoTokenProvider, IDisposable
{
    public const string HttpClientName = "ZohoAccounts";
    private const int TokenSafetyWindowSeconds = 60;

    private readonly IHttpClientFactory _http;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ZohoTokenProvider> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _cachedExpiresAt = DateTimeOffset.MinValue;
    private string? _cachedClientIdSig;

    public ZohoTokenProvider(
        IHttpClientFactory http,
        IServiceScopeFactory scopeFactory,
        ILogger<ZohoTokenProvider> logger)
    {
        _http = http;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        var (region, clientId, clientSecret, refreshToken) = await ReadCredentialsAsync(ct);

        if (!forceRefresh
            && _cachedToken is not null
            && DateTimeOffset.UtcNow < _cachedExpiresAt
            && _cachedClientIdSig == ClientIdSig(clientId))
        {
            return _cachedToken;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (!forceRefresh
                && _cachedToken is not null
                && DateTimeOffset.UtcNow < _cachedExpiresAt
                && _cachedClientIdSig == ClientIdSig(clientId))
            {
                return _cachedToken;
            }

            var (token, expiresIn) = await ExchangeRefreshTokenAsync(region, clientId, clientSecret, refreshToken, ct);
            _cachedToken = token;
            _cachedExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn).Subtract(TimeSpan.FromSeconds(TokenSafetyWindowSeconds));
            _cachedClientIdSig = ClientIdSig(clientId);
            return _cachedToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<(string Region, string ClientId, string ClientSecret, string RefreshToken)> ReadCredentialsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var connections = scope.ServiceProvider.GetRequiredService<IZohoConnectionService>();
        return await connections.GetCredentialsAsync(ct);
    }

    private async Task<(string accessToken, TimeSpan expiresIn)> ExchangeRefreshTokenAsync(
        string region, string clientId, string clientSecret, string refreshToken, CancellationToken ct)
    {
        var client = _http.CreateClient(HttpClientName);
        var requestUri = $"https://accounts.zoho.{region}/oauth/v2/token";

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("client_id",     clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type",    "refresh_token"),
        });

        using var response = await client.PostAsync(requestUri, form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Zoho token endpoint returned {Status}: {Body}",
                (int)response.StatusCode, Truncate(body, 1000));
            throw new InvalidOperationException(
                $"Zoho token exchange failed ({(int)response.StatusCode}): {Truncate(body, 500)}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errEl))
        {
            var err = errEl.GetString() ?? "unknown";
            throw new InvalidOperationException($"Zoho token exchange error: {err}");
        }

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

    public void Dispose() => _gate.Dispose();

    private static string ClientIdSig(string clientId) => clientId.Length <= 12 ? clientId : clientId[^12..];

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) || s!.Length <= max ? s ?? "" : s[..max];
}
