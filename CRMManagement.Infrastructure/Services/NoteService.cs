using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class NoteService : INoteService
{
    private readonly AppDbContext _db;
    public NoteService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<NoteDto>> ListAsync(string relatedType, Guid relatedId, CancellationToken ct) =>
        await _db.Notes.AsNoTracking()
            .Where(n => n.RelatedType == relatedType && n.RelatedId == relatedId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteDto(n.Id, n.Body, n.RelatedType, n.RelatedId, n.AuthorUserId, n.CreatedAt))
            .ToListAsync(ct);

    public async Task<NoteDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var n = await _db.Notes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return n == null ? null : new NoteDto(n.Id, n.Body, n.RelatedType, n.RelatedId, n.AuthorUserId, n.CreatedAt);
    }

    public async Task<Guid> UpsertAsync(NoteUpsertDto dto, CancellationToken ct)
    {
        Note e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct)
                ?? throw new InvalidOperationException("Note not found");
        }
        else
        {
            e = new Note { Id = Guid.NewGuid() };
            _db.Notes.Add(e);
        }
        e.Body = dto.Body;
        e.RelatedType = dto.RelatedType;
        e.RelatedId = dto.RelatedId;
        e.AuthorUserId = dto.AuthorUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Notes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (e != null)
        {
            _db.Notes.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
