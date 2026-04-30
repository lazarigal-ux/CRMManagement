using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserAdminListItemDto>> GetUsersAsync(CancellationToken cancellationToken);
    Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    Task SetUserRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken);
}
