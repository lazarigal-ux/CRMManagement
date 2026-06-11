using System.Net.Http.Headers;
using System.Text.Json;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRMManagement.Infrastructure.Services;

public sealed class ZohoConnectionService : IZohoConnectionService
{
    private const string ProtectorPurpose = "Zoho.Tokens";
    private static readonly string[] Scopes = new[]
    {
        "ZohoCRM.modules.READ",
        "ZohoCRM.users.READ",
        "ZohoCRM.settings.READ",
    };

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly IDataProtector _protector;
    private readonly ITokenStore _tokens;
    private readonly ILogger<ZohoConnectionService> _logger;

    public ZohoConnectionService(
        AppDbContext db,
        IHttpClientFactory http,
        IDataProtectionProvider protectionProvider,
        ITokenStore tokens,
        ILogger<ZohoConnectionService> logger)
    {
        _db = db;
        _http = http;
        _protector = protectionProvider.CreateProtector(ProtectorPurpose);
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<ZohoConnectionDto?> GetAsync(CancellationToken ct)
    {
        var row = await _db.ZohoConnections.AsNoTracking().FirstOrDefaultAsync(ct);
        return row is null ? null : ToDto(row);
    }

    public async Task SaveAppCredentialsAsync(string region, string clientId, string clientSecret, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(region))      throw new ArgumentException("Region is required.",       nameof(region));
        if (string.IsNullOrWhiteSpace(clientId))    throw new ArgumentException("Client ID is required.",    nameof(clientId));
        if (string.IsNullOrWhiteSpace(clientSecret))throw new ArgumentException("Client Secret is required.",nameof(clientSecret));

        var row = await _db.ZohoConnections.FirstOrDefaultAsync(ct);
        var protectedSecret = _protector.Protect(clientSecret);

        if (row is null)
        {
            row = new ZohoConnection
            {
                Id = Guid.NewGuid(),
                Region = region.Trim(),
                ClientId = clientId.Trim(),
                ClientSecretProtected = protectedSecret,
                Status = "Pending",
            };
            _db.ZohoConnections.Add(row);
        }
        else
        {
            row.Region = region.Trim();
            row.ClientId = clientId.Trim();
            row.ClientSecretProtected = protectedSecret;
            // Re-saving credentials invalidates any old refresh token.
            row.RefreshTokenProtected = null;
            row.AccountOwnerEmail = null;
            row.AccountOwnerName = null;
            row.ConnectedAt = null;
            row.DisconnectedAt = null;
            row.Status = "Pending";
            row.LastError = null;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<string> BuildAuthorizeUrlAsync(string callbackUrl, string state, CancellationToken ct)
    {
        var row = await _db.ZohoConnections.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Zoho app credentials are not configured yet.");

        var scope = string.Join(",", Scopes);
        var qs =
            $"response_type=code" +
            $"&client_id={Uri.EscapeDataString(row.ClientId)}" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
            $"&access_type=offline" +
            $"&prompt=consent" +
            $"&state={Uri.EscapeDataString(state)}";

        return $"https://accounts.zoho.{row.Region}/oauth/v2/auth?{qs}";
    }

    public async Task<ZohoConnectionDto> CompleteCallbackAsync(string code, string callbackUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Authorization code is required.", nameof(code));

        var row = await _db.ZohoConnections.FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Zoho app credentials are not configured yet.");

        var clientSecret = _protector.Unprotect(row.ClientSecretProtected);

        var tokenUrl = $"https://accounts.zoho.{row.Region}/oauth/v2/token";
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("grant_type",   "authorization_code"),
            new KeyValuePair<string,string>("client_id",    row.ClientId),
            new KeyValuePair<string,string>("client_secret",clientSecret),
            new KeyValuePair<string,string>("redirect_uri", callbackUrl),
            new KeyValuePair<string,string>("code",         code),
        });

        var client = _http.CreateClient(ZohoTokenProvider.HttpClientName);
        using var response = await client.PostAsync(tokenUrl, form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            row.Status = "Error";
            row.LastError = $"Token exchange {(int)response.StatusCode}: {Truncate(body, 1500)}";
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException(row.LastError);
        }

        using var doc = JsonDocument.Parse(body);
        var rootEl = doc.RootElement;

        if (rootEl.TryGetProperty("error", out var errEl))
        {
            row.Status = "Error";
            row.LastError = $"Token exchange error: {errEl.GetString()}";
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException(row.LastError);
        }

        if (!rootEl.TryGetProperty("refresh_token", out var refreshEl) || refreshEl.ValueKind != JsonValueKind.String)
        {
            row.Status = "Error";
            row.LastError = "Token response missing refresh_token. Did you include access_type=offline and prompt=consent?";
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException(row.LastError);
        }

        row.RefreshTokenProtected = _protector.Protect(refreshEl.GetString()!);
        row.Status = "Connected";
        row.ConnectedAt = DateTime.UtcNow;
        row.DisconnectedAt = null;
        row.LastError = null;

        var accessToken = rootEl.TryGetProperty("access_token", out var atEl) ? atEl.GetString() : null;
        if (!string.IsNullOrEmpty(accessToken))
        {
            var expiresIn = TimeSpan.FromSeconds(3600);
            if (rootEl.TryGetProperty("expires_in", out var expEl) && expEl.ValueKind == JsonValueKind.Number)
            {
                var raw = expEl.GetInt64();
                expiresIn = raw > 86400 ? TimeSpan.FromMilliseconds(raw) : TimeSpan.FromSeconds(raw);
            }
            await _tokens.SaveAsync(row.Id, ZohoTokenService.AccessTokenType, accessToken,
                DateTime.UtcNow + expiresIn - TimeSpan.FromMinutes(5), ct);

            try
            {
                var (email, name) = await FetchCurrentUserAsync(row.Region, accessToken!, ct);
                row.AccountOwnerEmail = email;
                row.AccountOwnerName = name;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch Zoho current user info; continuing without it.");
            }
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(row);
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        var row = await _db.ZohoConnections.FirstOrDefaultAsync(ct);
        if (row is null) return;

        row.RefreshTokenProtected = null;
        row.Status = "Disconnected";
        row.DisconnectedAt = DateTime.UtcNow;
        await _tokens.DeleteAsync(row.Id, ZohoTokenService.AccessTokenType, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string Region, string ClientId, string ClientSecret, string RefreshToken)> GetCredentialsAsync(CancellationToken ct)
    {
        var row = await _db.ZohoConnections.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Zoho not configured");

        if (string.IsNullOrEmpty(row.RefreshTokenProtected))
            throw new InvalidOperationException("Zoho not connected (no refresh token).");

        var clientSecret = _protector.Unprotect(row.ClientSecretProtected);
        var refreshToken = _protector.Unprotect(row.RefreshTokenProtected);
        return (row.Region, row.ClientId, clientSecret, refreshToken);
    }

    public async Task MarkImportedAsync(DateTime atUtc, CancellationToken ct)
    {
        var row = await _db.ZohoConnections.FirstOrDefaultAsync(ct);
        if (row is null) return;
        row.LastImportAt = atUtc;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<(string? email, string? name)> FetchCurrentUserAsync(string region, string accessToken, CancellationToken ct)
    {
        var client = _http.CreateClient(ZohoCrmReader.HttpClientName);
        var uri = new Uri($"https://www.zohoapis.{region}/crm/v6/users?type=CurrentUser");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return (null, null);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        if (!doc.RootElement.TryGetProperty("users", out var users) || users.ValueKind != JsonValueKind.Array) return (null, null);

        foreach (var u in users.EnumerateArray())
        {
            var email = u.TryGetProperty("email", out var e) ? e.GetString() : null;
            var name  = u.TryGetProperty("full_name", out var n) ? n.GetString() : null;
            return (email, name);
        }
        return (null, null);
    }

    private static ZohoConnectionDto ToDto(ZohoConnection row) => new(
        row.Id,
        row.Region,
        row.ClientId,
        !string.IsNullOrEmpty(row.ClientSecretProtected),
        !string.IsNullOrEmpty(row.RefreshTokenProtected),
        row.AccountOwnerEmail,
        row.AccountOwnerName,
        row.ConnectedAt,
        row.DisconnectedAt,
        row.LastImportAt,
        row.Status,
        row.LastError);

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
