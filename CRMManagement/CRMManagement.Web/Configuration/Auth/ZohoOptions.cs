using System.ComponentModel.DataAnnotations;

namespace CRMManagement.Web.Configuration.Auth;

public sealed class ZohoOptions
{
    public const string SectionName = "Zoho";

    public bool Enabled { get; init; }

    public string? ClientId { get; init; }

    public string? ClientSecret { get; init; }

    [Url]
    public string? RedirectUri { get; init; }

    public string AccountsUrl { get; init; } = "https://accounts.zoho.com";

    public string[] Scopes { get; init; } = ["ZohoCRM.modules.ALL", "ZohoCRM.settings.ALL"];
}
