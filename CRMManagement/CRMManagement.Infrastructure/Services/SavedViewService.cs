using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class SavedViewService : ISavedViewService
{
    private readonly AppDbContext _db;
    public SavedViewService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<SavedViewDto>> ListAsync(string entityType, Guid? userId, CancellationToken ct)
    {
        return await _db.Set<SavedView>().AsNoTracking()
            .Where(v => v.EntityType == entityType && (v.IsShared || v.OwnerUserId == userId))
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Name)
            .Select(v => new SavedViewDto(v.Id, v.EntityType, v.Name, v.OwnerUserId, v.ViewMode, v.FiltersJson, v.ColumnsJson, v.IsShared, v.IsDefault))
            .ToListAsync(ct);
    }

    public async Task<SavedViewDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var v = await _db.Set<SavedView>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return v is null ? null : new SavedViewDto(v.Id, v.EntityType, v.Name, v.OwnerUserId, v.ViewMode, v.FiltersJson, v.ColumnsJson, v.IsShared, v.IsDefault);
    }

    public async Task<Guid> UpsertAsync(SavedViewUpsertDto dto, CancellationToken ct)
    {
        SavedView entity;
        if (dto.Id is { } id)
        {
            entity = await _db.Set<SavedView>().FirstAsync(x => x.Id == id, ct);
        }
        else
        {
            entity = new SavedView { Id = Guid.NewGuid() };
            _db.Add(entity);
        }

        entity.EntityType = dto.EntityType;
        entity.Name = dto.Name;
        entity.OwnerUserId = dto.OwnerUserId;
        entity.ViewMode = dto.ViewMode;
        entity.FiltersJson = dto.FiltersJson;
        entity.ColumnsJson = dto.ColumnsJson;
        entity.IsShared = dto.IsShared;
        entity.IsDefault = dto.IsDefault;

        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Set<SavedView>().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return;
        _db.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
