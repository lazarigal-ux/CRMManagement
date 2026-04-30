using System.ComponentModel.DataAnnotations;

namespace CRMManagement.Web.Configuration.Auth;

public sealed class OidcAuthOptions
{
    public const string SectionName = "Authentication:Oidc";

    public bool Enabled { get; init; }

    [Url]
    public string? Authority { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    public string NameClaimType { get; init; } = "preferred_username";

    public string RoleClaimType { get; init; } = "roles";
}
