using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

/// <summary>
/// Resolves the current user ID from <see cref="ClaimsPrincipal"/>.
/// When no user is authenticated (local dev), falls back to the seeded "admin" user.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly AppDbContext _db;

    public CurrentUserService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> GetCurrentUserIdAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default)
    {
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var idValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(idValue, out var id))
            {
                return id;
            }
        }

        // Fallback: seeded admin user (development convenience).
        var adminId = await _db.Users
            .AsNoTracking()
            .Where(u => u.UserName == "admin")
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminId == Guid.Empty)
        {
            throw new InvalidOperationException("No authenticated user and admin user not found.");
        }

        return adminId;
    }
}
