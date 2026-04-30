using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class TicketService : ITicketService
{
    private readonly AppDbContext _db;
    public TicketService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<TicketListItemDto>> ListAsync(CancellationToken ct)
    {
        return await _db.Tickets.AsNoTracking()
            .OrderByDescending(t => t.OpenedAt)
            .Select(t => new TicketListItemDto(t.Id, t.TicketNumber, t.Subject, t.Status, t.Priority, t.Type, t.AccountId, t.OpenedAt))
            .ToListAsync(ct);
    }

    public async Task<TicketDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var x = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (x == null) return null;
        return new TicketDetailDto(x.Id, x.TicketNumber, x.Subject, x.Description, x.AccountId, x.ContactId, x.Status, x.Priority, x.Type, x.Channel, x.OwnerUserId, x.OpenedAt, x.ResolvedAt, x.ClosedAt);
    }

    public async Task<Guid> UpsertAsync(TicketUpsertDto dto, CancellationToken ct)
    {
        Ticket e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct)
                ?? throw new InvalidOperationException("Ticket not found");
        }
        else
        {
            e = new Ticket { Id = Guid.NewGuid(), OpenedAt = DateTime.UtcNow };
            _db.Tickets.Add(e);
        }
        e.TicketNumber = dto.TicketNumber;
        e.Subject = dto.Subject;
        e.Description = dto.Description;
        e.AccountId = dto.AccountId;
        e.ContactId = dto.ContactId;
        e.Status = dto.Status;
        e.Priority = dto.Priority;
        e.Type = dto.Type;
        e.Channel = dto.Channel;
        e.OwnerUserId = dto.OwnerUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (e != null)
        {
            _db.Tickets.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task CloseTicketAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new InvalidOperationException("Ticket not found");
        e.Status = "Closed";
        e.ClosedAt = DateTime.UtcNow;
        if (e.ResolvedAt == null) e.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
