using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class ActivityService : IActivityService
{
    private readonly AppDbContext _db;
    public ActivityService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ActivityListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Activities.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityListItemDto(a.Id, a.Type, a.Subject, a.Status, a.DueDate, a.OwnerUserId, a.RelatedType, a.RelatedId)
            {
                Priority            = a.Priority,
                Description         = a.Description,
                StartAt             = a.StartAt,
                EndAt               = a.EndAt,
                Location            = a.Location,
                CompletedAt         = a.CompletedAt,
                ActivityType        = a.ActivityType,
                CallType            = a.CallType,
                CallDurationSeconds = a.CallDurationSeconds,
                EventTitle          = a.EventTitle,
                ZohoCreatedTime     = a.ZohoCreatedTime,
                ZohoModifiedTime    = a.ZohoModifiedTime,
                OwnerName           = a.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == a.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);
    }

    public async Task<ActivityDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Activities.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (x == null) return null;
        return new ActivityDetailDto(x.Id, x.Type, x.Subject, x.Description, x.StartAt, x.EndAt, x.DueDate, x.Status, x.Priority, x.OwnerUserId, x.RelatedType, x.RelatedId, x.Location, x.CompletedAt);
    }

    public async Task<Guid> UpsertAsync(ActivityUpsertDto dto, CancellationToken ct)
    {
        Activity e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
                ?? throw new InvalidOperationException("Activity not found");
        }
        else
        {
            e = new Activity { Id = Guid.NewGuid() };
            _db.Activities.Add(e);
        }
        e.Type = dto.Type;
        e.Subject = dto.Subject;
        e.Description = dto.Description;
        e.StartAt = dto.StartAt;
        e.EndAt = dto.EndAt;
        e.DueDate = dto.DueDate;
        e.Status = dto.Status;
        e.Priority = dto.Priority;
        e.OwnerUserId = dto.OwnerUserId;
        e.RelatedType = dto.RelatedType;
        e.RelatedId = dto.RelatedId;
        e.Location = dto.Location;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (e != null)
        {
            _db.Activities.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new InvalidOperationException("Activity not found");
        e.Status = "Completed";
        e.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
