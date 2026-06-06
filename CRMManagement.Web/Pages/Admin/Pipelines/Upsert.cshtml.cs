using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CRMManagement.Web.Pages.Admin.Pipelines;

[Authorize(Roles = "Admin")]
public sealed class UpsertModel : PageModel
{
    private readonly IPipelineService _svc;
    public UpsertModel(IPipelineService svc) => _svc = svc;

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public bool IsDefault { get; set; }
    [BindProperty] public int SortOrder { get; set; }
    [BindProperty] public List<PipelineStageUpsertDto> Stages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken ct)
    {
        if (id is { } gid)
        {
            var d = await _svc.GetAsync(gid, ct);
            if (d is null) return NotFound();
            Id = d.Id;
            Name = d.Name;
            Description = d.Description;
            IsDefault = d.IsDefault;
            SortOrder = d.SortOrder;
            Stages = d.Stages.Select(s => new PipelineStageUpsertDto(s.Id, s.Name, s.SortOrder, s.Probability, s.IsWon, s.IsLost)).ToList();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var dto = new PipelineUpsertDto(Id, Name, Description, IsDefault, SortOrder, Stages);
        await _svc.UpsertAsync(dto, ct);
        return RedirectToPage("/Admin/Pipelines/Index");
    }
}
