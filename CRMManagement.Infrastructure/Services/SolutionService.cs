using CRMManagement.Application.Abstractions;
using CRMManagement.Application.DTOs;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class SolutionService : ISolutionService
{
    private readonly AppDbContext _db;
    public SolutionService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SolutionListItemDto>> ListAsync(CancellationToken ct) =>
        await _db.Solutions.AsNoTracking()
            .OrderBy(s => s.Title)
            .Select(s => new SolutionListItemDto(s.Id, s.SolutionNumber, s.Title, s.Category, s.Status, s.Published)
            {
                Question         = s.Question,
                Answer           = s.Answer,
                Comments         = s.Comments,
                ZohoCreatedTime  = s.ZohoCreatedTime,
                ZohoModifiedTime = s.ZohoModifiedTime,
                ProductName      = s.ProductId == null
                    ? null
                    : _db.Products.Where(p => p.Id == s.ProductId).Select(p => p.Name).FirstOrDefault(),
                OwnerName        = s.OwnerUserId == null
                    ? null
                    : _db.Users.Where(u => u.Id == s.OwnerUserId).Select(u => u.UserName).FirstOrDefault(),
            })
            .ToListAsync(ct);

    public async Task<SolutionDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var s = await _db.Solutions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return s is null ? null
            : new SolutionDetailDto(s.Id, s.SolutionNumber, s.Title, s.Question, s.Answer, s.Category,
                s.Status, s.ProductId, s.Published, s.Comments, s.OwnerUserId);
    }

    public async Task<Guid> UpsertAsync(SolutionUpsertDto dto, CancellationToken ct)
    {
        Solution e;
        if (dto.Id is Guid id && id != Guid.Empty)
        {
            e = await _db.Solutions.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Solution not found");
        }
        else
        {
            e = new Solution { Id = Guid.NewGuid() };
            _db.Solutions.Add(e);
        }
        e.SolutionNumber = dto.SolutionNumber;
        e.Title = dto.Title;
        e.Question = dto.Question;
        e.Answer = dto.Answer;
        e.Category = dto.Category;
        e.Status = dto.Status;
        e.ProductId = dto.ProductId;
        e.Published = dto.Published;
        e.Comments = dto.Comments;
        e.OwnerUserId = dto.OwnerUserId;
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var s = await _db.Solutions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return;
        _db.Solutions.Remove(s);
        await _db.SaveChangesAsync(ct);
    }
}
