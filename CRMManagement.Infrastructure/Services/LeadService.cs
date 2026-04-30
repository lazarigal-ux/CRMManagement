using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class LeadService : ILeadService
{
    private readonly AppDbContext _db;
    public LeadService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<LeadListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Leads.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadListItemDto(x.Id, x.FirstName, x.LastName, x.Company, x.Email, x.Status, x.Rating, x.Score, x.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Leads.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, ct);
        if (x == null) return null;
        return new LeadDetailDto(x.Id, x.FirstName, x.LastName, x.Company, x.Title, x.Email, x.Phone, x.Source, x.Status, x.Rating, x.Score, x.OwnerUserId, x.Industry, x.Website, x.Description, x.ConvertedAccountId, x.ConvertedContactId, x.ConvertedOpportunityId, x.ConvertedAt);
    }

    public async Task<Guid> UpsertAsync(LeadUpsertDto dto, CancellationToken ct)
    {
        Lead entity;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            entity = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct)
                ?? throw new InvalidOperationException("Lead not found");
        }
        else
        {
            entity = new Lead { Id = Guid.NewGuid() };
            _db.Leads.Add(entity);
        }
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Company = dto.Company;
        entity.Title = dto.Title;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Source = dto.Source;
        entity.Status = dto.Status;
        entity.Rating = dto.Rating;
        entity.Score = dto.Score;
        entity.OwnerUserId = dto.OwnerUserId;
        entity.Industry = dto.Industry;
        entity.Website = dto.Website;
        entity.Description = dto.Description;
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (e != null)
        {
            _db.Leads.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<LeadConversionResultDto> ConvertLeadAsync(Guid id, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new InvalidOperationException("Lead not found");

        var pipeline = await _db.Pipelines.Include(p => p.Stages)
            .Where(p => p.IsDefault)
            .FirstOrDefaultAsync(ct)
            ?? await _db.Pipelines.Include(p => p.Stages).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No pipeline available");
        var firstStage = pipeline.Stages.OrderBy(s => s.SortOrder).FirstOrDefault()
            ?? throw new InvalidOperationException("Pipeline has no stages");

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(lead.Company) ? $"{lead.FirstName} {lead.LastName}".Trim() : lead.Company!,
            Industry = lead.Industry,
            Website = lead.Website,
            IsActive = true,
            OwnerUserId = lead.OwnerUserId
        };
        _db.Accounts.Add(account);

        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            Phone = lead.Phone,
            Title = lead.Title,
            AccountId = account.Id,
            OwnerUserId = lead.OwnerUserId
        };
        _db.Contacts.Add(contact);

        var opportunity = new Opportunity
        {
            Id = Guid.NewGuid(),
            Name = $"{account.Name} - Opportunity",
            AccountId = account.Id,
            ContactId = contact.Id,
            PipelineId = pipeline.Id,
            StageId = firstStage.Id,
            Amount = 0m,
            Currency = "USD",
            Status = "Open",
            Probability = firstStage.Probability,
            LeadSource = lead.Source,
            OwnerUserId = lead.OwnerUserId
        };
        _db.Opportunities.Add(opportunity);

        lead.Status = "Converted";
        lead.ConvertedAt = DateTime.UtcNow;
        lead.ConvertedAccountId = account.Id;
        lead.ConvertedContactId = contact.Id;
        lead.ConvertedOpportunityId = opportunity.Id;

        await _db.SaveChangesAsync(ct);

        return new LeadConversionResultDto(lead.Id, account.Id, contact.Id, opportunity.Id);
    }
}
