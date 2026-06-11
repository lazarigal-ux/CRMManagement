using CRMManagement.Application.DTOs;

namespace CRMManagement.Web.Pages.Shared;

public sealed class SavedViewsBarModel
{
    public string EntityType { get; init; } = "";
    public string ViewMode { get; init; } = "list";
    public Guid? CurrentViewId { get; init; }
    public IReadOnlyList<SavedViewDto> Views { get; init; } = Array.Empty<SavedViewDto>();
}
