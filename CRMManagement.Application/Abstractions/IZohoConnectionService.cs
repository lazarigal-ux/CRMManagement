using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

public interface IZohoConnectionService
{
    Task<ZohoConnectionDto?> GetAsync(CancellationToken ct);

    Task SaveAppCredentialsAsync(string region, string clientId, string clientSecret, CancellationToken ct);

    /// <summary>Returns the URL the browser should be redirected to so the user can authorize on Zoho.</summary>
    Task<string> BuildAuthorizeUrlAsync(string callbackUrl, string state, CancellationToken ct);

    /// <summary>Exchanges the authorization code for a refresh token and persists it. Returns the updated connection.</summary>
    Task<ZohoConnectionDto> CompleteCallbackAsync(string code, string callbackUrl, CancellationToken ct);

    Task DisconnectAsync(CancellationToken ct);

    /// <summary>For ZohoTokenProvider — reads the persisted client_id/secret/refresh_token. Throws if not configured.</summary>
    Task<(string Region, string ClientId, string ClientSecret, string RefreshToken)> GetCredentialsAsync(CancellationToken ct);

    Task MarkImportedAsync(DateTime atUtc, CancellationToken ct);
}
