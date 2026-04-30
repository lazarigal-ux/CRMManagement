using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class OpportunityService : IOpportunityService
{
    private readonly AppDbContext _db;
    public OpportunityService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<OpportunityListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Opportunities.AsNoTracking()
            .OrderByDescending(o => o.Amount)
            .Select(o => new OpportunityListItemDto(o.Id, o.Name, o.AccountId, o.Amount, o.Currency, o.StageId, o.Status, o.CloseDate))
            .ToListAsync(ct);
    }

    public async Task<OpportunityDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Opportunities.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
        if (x == null) return null;
        return new OpportunityDetailDto(x.Id, x.Name, x.AccountId, x.ContactId, x.PipelineId, x.StageId, x.Amount, x.Currency, x.CloseDate, x.Probability, x.Status, x.LeadSource, x.OwnerUserId, x.Description, x.NextStep);
    }

    public async Task<Guid> UpsertAsync(OpportunityUpsertDto dto, CancellationToken ct)
    {
        Opportunity e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == id, ct)
                ?? throw new InvalidOperationException("Opportunity not found");
        }
        else
        {
            e = new Opportunity { Id = Guid.NewGuid() };
            _db.Opportunities.Add(e);
        }
        e.Name = dto.Name;
        e.AccountId = dto.AccountId;
        e.ContactId = dto.ContactId;
        e.PipelineId = dto.PipelineId;
        e.StageId = dto.StageId;
        e.Amount = dto.Amount;
        e.Currency = dto.Currency;
        e.CloseDate = dto.CloseDate;
        e.Probability = dto.Probability;
        e.Status = dto.Status;
        e.LeadSource = dto.LeadSource;
        e.OwnerUserId = dto.OwnerUserId;
        e.Description = dto.Description;
        e.NextStep = dto.NextStep;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (e != null)
        {
            _db.Opportunities.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AdvanceStageAsync(Guid id, Guid stageId, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new InvalidOperationException("Opportunity not found");
        var stage = await _db.PipelineStages.AsNoTracking().FirstOrDefaultAsync(s => s.Id == stageId, ct)
            ?? throw new InvalidOperationException("Stage not found");
        opp.StageId = stage.Id;
        opp.Probability = stage.Probability;
        if (stage.IsWon) opp.Status = "Won";
        else if (stage.IsLost) opp.Status = "Lost";
        else opp.Status = "Open";
        await _db.SaveChangesAsync(ct);
    }
}
