using Microsoft.AspNetCore.Identity;

namespace CRMManagement.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
