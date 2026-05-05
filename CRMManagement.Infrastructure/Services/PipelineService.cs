using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class PipelineService : IPipelineService
{
    private readonly AppDbContext _db;
    public PipelineService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PipelineDto>> ListAsync(CancellationToken ct)
    {
        var pipelines = await _db.Pipelines.AsNoTracking()
            .Include(p => p.Stages)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .ToListAsync(ct);
        return pipelines.Select(Map).ToList();
    }

    public async Task<PipelineDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.Pipelines.AsNoTracking().Include(x => x.Stages).FirstOrDefaultAsync(x => x.Id == id, ct);
        return p == null ? null : Map(p);
    }

    public async Task<Guid> UpsertAsync(PipelineUpsertDto dto, CancellationToken ct)
    {
        Pipeline p;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            p = await _db.Pipelines.Include(x => x.Stages).FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Pipeline not found");
        }
        else
        {
            p = new Pipeline { Id = Guid.NewGuid() };
            _db.Pipelines.Add(p);
        }
        p.Name = dto.Name;
        p.Description = dto.Description;
        p.IsDefault = dto.IsDefault;
        p.SortOrder = dto.SortOrder;

        _db.PipelineStages.RemoveRange(p.Stages);
        foreach (var s in dto.Stages)
        {
            _db.PipelineStages.Add(new PipelineStage
            {
                Id = Guid.NewGuid(),
                PipelineId = p.Id,
                Name = s.Name,
                SortOrder = s.SortOrder,
                Probability = s.Probability,
                IsWon = s.IsWon,
                IsLost = s.IsLost
            });
        }

        await _db.SaveChangesAsync(ct);
        return p.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var p = await _db.Pipelines.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p != null)
        {
            _db.Pipelines.Remove(p);
            await _db.SaveChangesAsync(ct);
        }
    }

    private static PipelineDto Map(Pipeline p) =>
        new(p.Id, p.Name, p.Description, p.IsDefault, p.SortOrder,
            p.Stages.OrderBy(s => s.SortOrder)
                .Select(s => new PipelineStageDto(s.Id, s.PipelineId, s.Name, s.SortOrder, s.Probability, s.IsWon, s.IsLost))
                .ToList());
}
