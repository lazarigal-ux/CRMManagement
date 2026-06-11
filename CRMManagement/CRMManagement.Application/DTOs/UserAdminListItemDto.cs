namespace CRMManagement.Application.DTOs;

public sealed record UserAdminListItemDto(
    Guid Id,
    string UserName,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
