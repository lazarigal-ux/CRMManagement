using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class CampaignService : ICampaignService
{
    private readonly AppDbContext _db;
    public CampaignService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<CampaignListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Campaigns.AsNoTracking()
            .OrderByDescending(c => c.StartDate)
            .Select(c => new CampaignListItemDto(c.Id, c.Name, c.Type, c.Status, c.StartDate, c.EndDate)
            {
                BudgetedCost     = c.BudgetedCost,
                ActualCost       = c.ActualCost,
                ExpectedRevenue  = c.ExpectedRevenue,
                Description      = c.Description,
                NumSent          = c.NumSent,
                ZohoCreatedTime  = c.ZohoCreatedTime,
                ZohoModifiedTime = c.ZohoModifiedTime,
                OwnerName        = c.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == c.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);
    }

    public async Task<CampaignDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (x == null) return null;
        return new CampaignDetailDto(x.Id, x.Name, x.Type, x.Status, x.StartDate, x.EndDate, x.BudgetedCost, x.ActualCost, x.ExpectedRevenue, x.Description, x.OwnerUserId);
    }

    public async Task<Guid> UpsertAsync(CampaignUpsertDto dto, CancellationToken ct)
    {
        Campaign e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new InvalidOperationException("Campaign not found");
        }
        else
        {
            e = new Campaign { Id = Guid.NewGuid() };
            _db.Campaigns.Add(e);
        }
        e.Name = dto.Name;
        e.Type = dto.Type;
        e.Status = dto.Status;
        e.StartDate = dto.StartDate;
        e.EndDate = dto.EndDate;
        e.BudgetedCost = dto.BudgetedCost;
        e.ActualCost = dto.ActualCost;
        e.ExpectedRevenue = dto.ExpectedRevenue;
        e.Description = dto.Description;
        e.OwnerUserId = dto.OwnerUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (e != null)
        {
            _db.Campaigns.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
