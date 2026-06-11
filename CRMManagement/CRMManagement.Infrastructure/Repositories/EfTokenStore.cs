using CRMManagement.Application.Abstractions;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Repositories;

public sealed class EfTokenStore : ITokenStore
{
    private readonly AppDbContext _db;

    public EfTokenStore(AppDbContext db) => _db = db;

    public async Task SaveAsync(Guid userId, string tokenType, string value, DateTime expiresAt, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenType == tokenType)
            .ExecuteDeleteAsync(ct);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = tokenType,
            Value = value,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public Task<string?> GetAsync(Guid userId, string tokenType, CancellationToken ct = default)
    {
        return _db.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenType == tokenType && t.ExpiresAt > DateTime.UtcNow)
            .Select(t => t.Value)
            .FirstOrDefaultAsync(ct);
    }

    public Task DeleteAsync(Guid userId, string tokenType, CancellationToken ct = default)
    {
        return _db.RefreshTokens
            .Where(t => t.UserId == userId && t.TokenType == tokenType)
            .ExecuteDeleteAsync(ct);
    }
}
