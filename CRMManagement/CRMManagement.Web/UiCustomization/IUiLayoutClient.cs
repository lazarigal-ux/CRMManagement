using System.Text.Json;

namespace CRMManagement.Web.UiCustomization;

public interface IUiLayoutClient
{
    Task<JsonElement?> GetLayoutAsync(string pageKey, string featureKey, CancellationToken ct);
    Task SaveLayoutAsync(string pageKey, string featureKey, JsonElement layoutJson, CancellationToken ct);
    Task ResetLayoutAsync(string pageKey, string featureKey, CancellationToken ct);
}
