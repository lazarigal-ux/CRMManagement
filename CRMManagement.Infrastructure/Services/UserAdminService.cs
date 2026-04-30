using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Infrastructure.Identity;

namespace CRMManagement.Infrastructure.Services;

public sealed class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UserAdminListItemDto>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);

        var result = new List<UserAdminListItemDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserAdminListItemDto(u.Id, u.UserName ?? "", u.Email ?? "", u.IsActive, roles.ToList()));
        }

        return result;
    }

    public async Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) throw new InvalidOperationException("User not found.");

        user.IsActive = isActive;
        await _userManager.UpdateAsync(user);
    }

    public async Task SetUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) throw new InvalidOperationException("User not found.");

        var current = await _userManager.GetRolesAsync(user);

        var desired = (roles ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var toRemove = current.Where(r => !desired.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();
        var toAdd = desired.Where(r => !current.Contains(r, StringComparer.OrdinalIgnoreCase)).ToList();

        if (toRemove.Count > 0) await _userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0) await _userManager.AddToRolesAsync(user, toAdd);
    }
}
