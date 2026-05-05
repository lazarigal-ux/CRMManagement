using System.ComponentModel.DataAnnotations;

namespace CRMManagement.Web.Configuration.Auth;

public sealed class GitHubCodeOptions
{
    public const string SectionName = "Authentication:GitHub";

    public bool Enabled { get; init; }

    [Required]
    public string? ClientId { get; init; }

    [Required]
    public string? ClientSecret { get; init; }
}
