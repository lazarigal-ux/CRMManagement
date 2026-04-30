using CRMManagement.Application.DTOs;

namespace CRMManagement.Application.Abstractions;

public interface ILeadService
{
    Task<IReadOnlyList<LeadListItemDto>> ListAsync(CancellationToken ct);
    Task<LeadDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(LeadUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<LeadConversionResultDto> ConvertLeadAsync(Guid id, CancellationToken ct);
}

public interface IAccountService
{
    Task<IReadOnlyList<AccountListItemDto>> ListAsync(CancellationToken ct);
    Task<AccountDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(AccountUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IContactService
{
    Task<IReadOnlyList<ContactListItemDto>> ListAsync(CancellationToken ct);
    Task<ContactDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(ContactUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IOpportunityService
{
    Task<IReadOnlyList<OpportunityListItemDto>> ListAsync(CancellationToken ct);
    Task<OpportunityDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(OpportunityUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AdvanceStageAsync(Guid id, Guid stageId, CancellationToken ct);
}

public interface IActivityService
{
    Task<IReadOnlyList<ActivityListItemDto>> ListAsync(CancellationToken ct);
    Task<ActivityDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(ActivityUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task CompleteAsync(Guid id, CancellationToken ct);
}

public interface IProductService
{
    Task<IReadOnlyList<ProductListItemDto>> ListAsync(CancellationToken ct);
    Task<ProductDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(ProductUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IQuoteService
{
    Task<IReadOnlyList<QuoteListItemDto>> ListAsync(CancellationToken ct);
    Task<QuoteDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(QuoteUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task AddQuoteLineAsync(Guid quoteId, QuoteLineUpsertDto line, CancellationToken ct);
}

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignListItemDto>> ListAsync(CancellationToken ct);
    Task<CampaignDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(CampaignUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface ITicketService
{
    Task<IReadOnlyList<TicketListItemDto>> ListAsync(CancellationToken ct);
    Task<TicketDetailDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(TicketUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task CloseTicketAsync(Guid id, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct);
}

public interface IPipelineService
{
    Task<IReadOnlyList<PipelineDto>> ListAsync(CancellationToken ct);
    Task<PipelineDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(PipelineUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> ListAsync(CancellationToken ct);
    Task<TagDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(TagUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface INoteService
{
    Task<IReadOnlyList<NoteDto>> ListAsync(string relatedType, Guid relatedId, CancellationToken ct);
    Task<NoteDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(NoteUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public interface IAttachmentService
{
    Task<IReadOnlyList<AttachmentDto>> ListAsync(string relatedType, Guid relatedId, CancellationToken ct);
    Task<AttachmentDto?> GetAsync(Guid id, CancellationToken ct);
    Task<Guid> UpsertAsync(AttachmentUpsertDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
