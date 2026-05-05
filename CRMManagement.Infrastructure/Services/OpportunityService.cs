using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class OpportunityService : IOpportunityService
{
    private readonly AppDbContext _db;
    private readonly IIntegrationOutboxService _outbox;
    public OpportunityService(AppDbContext db, IIntegrationOutboxService outbox)
    {
        _db = db;
        _outbox = outbox;
    }

    public async Task<IReadOnlyList<OpportunityListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Opportunities.AsNoTracking()
            .OrderByDescending(o => o.Amount)
            .Select(o => new OpportunityListItemDto(o.Id, o.Name, o.AccountId, o.Amount, o.Currency, o.StageId, o.Status, o.CloseDate)
            {
                Probability      = o.Probability,
                LeadSource       = o.LeadSource,
                Description      = o.Description,
                NextStep         = o.NextStep,
                Type             = o.Type,
                ZohoCreatedTime  = o.ZohoCreatedTime,
                ZohoModifiedTime = o.ZohoModifiedTime,
                StageName        = _db.PipelineStages.Where(s => s.Id == o.StageId).Select(s => s.Name).FirstOrDefault(),
                AccountName      = o.AccountId == null
                    ? null
                    : _db.Accounts.Where(a => a.Id == o.AccountId).Select(a => a.Name).FirstOrDefault(),
                OwnerName        = o.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == o.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
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
            e = new Opportunity
            {
                Id = Guid.NewGuid(),
                StageEnteredAt = DateTime.UtcNow,
            };
            _db.Opportunities.Add(e);
        }
        e.Name = dto.Name;
        e.AccountId = dto.AccountId;
        e.ContactId = dto.ContactId;
        e.PipelineId = dto.PipelineId;
        if (e.StageId != dto.StageId)
        {
            e.StageEnteredAt = DateTime.UtcNow;
        }
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
        if (opp.StageId != stage.Id)
        {
            opp.StageEnteredAt = DateTime.UtcNow;
        }
        opp.StageId = stage.Id;
        opp.Probability = stage.Probability;

        var wasNotWon = !string.Equals(opp.Status, "Won", StringComparison.OrdinalIgnoreCase);
        if (stage.IsWon) opp.Status = "Won";
        else if (stage.IsLost) opp.Status = "Lost";
        else opp.Status = "Open";
        await _db.SaveChangesAsync(ct);

        // Phase 7: on the won transition, queue a BOM hand-off to IMS.
        if (stage.IsWon && wasNotWon)
        {
            await EnqueueDealWonAsync(opp, ct);
        }
    }

    private async Task EnqueueDealWonAsync(Opportunity opp, CancellationToken ct)
    {
        // Pick the most-recent accepted (or otherwise highest) Quote for this opportunity.
        var quote = await _db.Quotes
            .AsNoTracking()
            .Where(q => q.OpportunityId == opp.Id)
            .OrderByDescending(q => q.AcceptedAt ?? q.UpdatedAt)
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(ct);

        var lines = (quote?.Lines ?? Enumerable.Empty<QuoteLine>())
            .OrderBy(l => l.SortOrder)
            .Select(l => new
            {
                productId = l.ProductId,
                description = l.Description,
                quantity = l.Quantity,
                unitPrice = l.UnitPrice,
                lineTotal = l.LineTotal,
            })
            .ToList();

        var payload = new
        {
            opportunityId = opp.Id,
            opportunityName = opp.Name,
            accountId = opp.AccountId,
            wonAt = DateTime.UtcNow,
            currency = opp.Currency,
            amount = opp.Amount,
            quoteId = (Guid?)(quote?.Id),
            quoteNumber = quote?.QuoteNumber,
            lines,
        };

        await _outbox.EnqueueAsync(
            target: "ims.deal-won",
            payload: payload,
            relatedType: "Opportunity",
            relatedId: opp.Id,
            ct: ct);
    }
}
