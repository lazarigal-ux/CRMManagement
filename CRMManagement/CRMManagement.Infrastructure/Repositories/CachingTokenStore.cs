using CRMManagement.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace CRMManagement.Infrastructure.Repositories;

public sealed class CachingTokenStore : ITokenStore
{
    private readonly EfTokenStore _inner;
    private readonly IMemoryCache _cache;

    public CachingTokenStore(EfTokenStore inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task SaveAsync(Guid userId, string tokenType, string value, DateTime expiresAt, CancellationToken ct = default)
    {
        await _inner.SaveAsync(userId, tokenType, value, expiresAt, ct);

        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            _cache.Set(CacheKey(userId, tokenType), value, ttl);
    }

    public async Task<string?> GetAsync(Guid userId, string tokenType, CancellationToken ct = default)
    {
        var key = CacheKey(userId, tokenType);

        if (_cache.TryGetValue(key, out string? cached))
            return cached;

        var value = await _inner.GetAsync(userId, tokenType, ct);

        if (value is not null)
            _cache.Set(key, value, TimeSpan.FromMinutes(5));

        return value;
    }

    public async Task DeleteAsync(Guid userId, string tokenType, CancellationToken ct = default)
    {
        await _inner.DeleteAsync(userId, tokenType, ct);
        _cache.Remove(CacheKey(userId, tokenType));
    }

    private static string CacheKey(Guid userId, string tokenType) =>
        $"token:{userId}:{tokenType}";
}
