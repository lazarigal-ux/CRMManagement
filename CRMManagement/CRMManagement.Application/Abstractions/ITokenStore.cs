namespace CRMManagement.Application.Abstractions;

public interface ITokenStore
{
    Task SaveAsync(Guid userId, string tokenType, string value, DateTime expiresAt, CancellationToken ct = default);
    Task<string?> GetAsync(Guid userId, string tokenType, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, string tokenType, CancellationToken ct = default);
}
