using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class TagService : ITagService
{
    private readonly AppDbContext _db;
    public TagService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken ct) =>
        await _db.Tags.AsNoTracking()
            .OrderBy(t => t.Scope).ThenBy(t => t.Name)
            .Select(t => new TagDto(t.Id, t.Name, t.Color, t.Scope))
            .ToListAsync(ct);

    public async Task<TagDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var t = await _db.Tags.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t == null ? null : new TagDto(t.Id, t.Name, t.Color, t.Scope);
    }

    public async Task<Guid> UpsertAsync(TagUpsertDto dto, CancellationToken ct)
    {
        Tag e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct)
                ?? throw new InvalidOperationException("Tag not found");
        }
        else
        {
            e = new Tag { Id = Guid.NewGuid() };
            _db.Tags.Add(e);
        }
        e.Name = dto.Name;
        e.Color = dto.Color;
        e.Scope = dto.Scope;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (e != null)
        {
            _db.Tags.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
