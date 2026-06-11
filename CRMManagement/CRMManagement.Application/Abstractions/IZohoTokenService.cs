namespace CRMManagement.Application.Abstractions;

public interface IZohoTokenService
{
    Task<string> GetAccessTokenAsync(Guid connectionId, CancellationToken ct = default);
}
