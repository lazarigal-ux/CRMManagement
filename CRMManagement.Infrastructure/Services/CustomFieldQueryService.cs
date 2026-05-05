using CRMManagement.Application.Abstractions;
using CRMManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CRMManagement.Infrastructure.Services;

public sealed class CustomFieldQueryService : ICustomFieldQueryService
{
    private readonly AppDbContext _db;
    public CustomFieldQueryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomFieldValueDto>> GetForEntityAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty)
            return Array.Empty<CustomFieldValueDto>();

        var rows = await (
            from f in _db.CustomFields.AsNoTracking()
            join v in _db.CustomFieldValues.AsNoTracking()
                on new { CfId = f.Id, EntId = entityId } equals new { CfId = v.CustomFieldId, EntId = v.EntityId }
            where f.EntityType == entityType
            orderby f.SortOrder, f.Label
            select new CustomFieldValueDto(f.Name, f.Label, f.DataType, v.ValueText)
        ).ToListAsync(ct);

        return rows;
    }
}
