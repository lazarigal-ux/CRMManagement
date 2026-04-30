using System.Security.Claims;

namespace CRMManagement.Application.Abstractions;

public interface ICurrentUserService
{
    Task<Guid> GetCurrentUserIdAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default);
}
