using Microsoft.EntityFrameworkCore;
using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;

namespace CRMManagement.Infrastructure.Services;

public sealed class AttachmentService : IAttachmentService
{
    private readonly AppDbContext _db;
    public AttachmentService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<AttachmentDto>> ListAsync(string relatedType, Guid relatedId, CancellationToken ct) =>
        await _db.Attachments.AsNoTracking()
            .Where(a => a.RelatedType == relatedType && a.RelatedId == relatedId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto(a.Id, a.FileName, a.StoragePath, a.ContentType, a.SizeBytes, a.RelatedType, a.RelatedId, a.UploadedByUserId, a.CreatedAt))
            .ToListAsync(ct);

    public async Task<AttachmentDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await _db.Attachments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a == null ? null : new AttachmentDto(a.Id, a.FileName, a.StoragePath, a.ContentType, a.SizeBytes, a.RelatedType, a.RelatedId, a.UploadedByUserId, a.CreatedAt);
    }

    public async Task<Guid> UpsertAsync(AttachmentUpsertDto dto, CancellationToken ct)
    {
        Attachment e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct)
                ?? throw new InvalidOperationException("Attachment not found");
        }
        else
        {
            e = new Attachment { Id = Guid.NewGuid() };
            _db.Attachments.Add(e);
        }
        e.FileName = dto.FileName;
        e.StoragePath = dto.StoragePath;
        e.ContentType = dto.ContentType;
        e.SizeBytes = dto.SizeBytes;
        e.RelatedType = dto.RelatedType;
        e.RelatedId = dto.RelatedId;
        e.UploadedByUserId = dto.UploadedByUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (e != null)
        {
            _db.Attachments.Remove(e);
            await _db.SaveChangesAsync(ct);
        }
    }
}
