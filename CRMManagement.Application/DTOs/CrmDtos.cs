namespace CRMManagement.Application.DTOs;

public sealed record LeadListItemDto(Guid Id, string FirstName, string LastName, string? Company, string? Email, string Status, string? Rating, int Score, DateTime CreatedAt);
public sealed record LeadDetailDto(Guid Id, string FirstName, string LastName, string? Company, string? Title, string? Email, string? Phone, string? Source, string Status, string? Rating, int Score, Guid? OwnerUserId, string? Industry, string? Website, string? Description, Guid? ConvertedAccountId, Guid? ConvertedContactId, Guid? ConvertedOpportunityId, DateTime? ConvertedAt);
public sealed record LeadUpsertDto(Guid? Id, string FirstName, string LastName, string? Company, string? Title, string? Email, string? Phone, string? Source, string Status, string? Rating, int Score, Guid? OwnerUserId, string? Industry, string? Website, string? Description);
public sealed record LeadConversionResultDto(Guid LeadId, Guid AccountId, Guid ContactId, Guid OpportunityId);

public sealed record AccountListItemDto(Guid Id, string Name, string? Industry, string? Website, string? Phone, bool IsActive);
public sealed record AccountDetailDto(Guid Id, string Name, string? LegalName, string? Industry, string? Website, string? Phone, string? Email, string? BillingAddress, string? ShippingAddress, decimal? AnnualRevenue, int? EmployeeCount, Guid? OwnerUserId, Guid? ParentAccountId, string? Description, bool IsActive);
public sealed record AccountUpsertDto(Guid? Id, string Name, string? LegalName, string? Industry, string? Website, string? Phone, string? Email, string? BillingAddress, string? ShippingAddress, decimal? AnnualRevenue, int? EmployeeCount, Guid? OwnerUserId, Guid? ParentAccountId, string? Description, bool IsActive);

public sealed record ContactListItemDto(Guid Id, string FirstName, string LastName, string? Email, string? Phone, Guid? AccountId);
public sealed record ContactDetailDto(Guid Id, string FirstName, string LastName, string? Title, string? Email, string? Phone, string? Mobile, Guid? AccountId, Guid? OwnerUserId, string? Department, string? Address, string? Description, bool IsPrimary, bool DoNotContact);
public sealed record ContactUpsertDto(Guid? Id, string FirstName, string LastName, string? Title, string? Email, string? Phone, string? Mobile, Guid? AccountId, Guid? OwnerUserId, string? Department, string? Address, string? Description, bool IsPrimary, bool DoNotContact);

public sealed record OpportunityListItemDto(Guid Id, string Name, Guid? AccountId, decimal Amount, string Currency, Guid StageId, string Status, DateTime? CloseDate);
public sealed record OpportunityDetailDto(Guid Id, string Name, Guid? AccountId, Guid? ContactId, Guid PipelineId, Guid StageId, decimal Amount, string Currency, DateTime? CloseDate, int Probability, string Status, string? LeadSource, Guid? OwnerUserId, string? Description, string? NextStep);
public sealed record OpportunityUpsertDto(Guid? Id, string Name, Guid? AccountId, Guid? ContactId, Guid PipelineId, Guid StageId, decimal Amount, string Currency, DateTime? CloseDate, int Probability, string Status, string? LeadSource, Guid? OwnerUserId, string? Description, string? NextStep);

public sealed record ActivityListItemDto(Guid Id, string Type, string Subject, string Status, DateTime? DueDate, Guid? OwnerUserId, string? RelatedType, Guid? RelatedId);
public sealed record ActivityDetailDto(Guid Id, string Type, string Subject, string? Description, DateTime? StartAt, DateTime? EndAt, DateTime? DueDate, string Status, string? Priority, Guid? OwnerUserId, string? RelatedType, Guid? RelatedId, string? Location, DateTime? CompletedAt);
public sealed record ActivityUpsertDto(Guid? Id, string Type, string Subject, string? Description, DateTime? StartAt, DateTime? EndAt, DateTime? DueDate, string Status, string? Priority, Guid? OwnerUserId, string? RelatedType, Guid? RelatedId, string? Location);

public sealed record ProductListItemDto(Guid Id, string Sku, string Name, string? Family, bool IsActive, decimal StandardPrice);
public sealed record ProductDetailDto(Guid Id, string Sku, string Name, string? Description, string? Family, bool IsActive, decimal StandardPrice, decimal? Cost, string? Unit);
public sealed record ProductUpsertDto(Guid? Id, string Sku, string Name, string? Description, string? Family, bool IsActive, decimal StandardPrice, decimal? Cost, string? Unit);

public sealed record QuoteListItemDto(Guid Id, string QuoteNumber, string Name, Guid? AccountId, string Status, decimal Total, string Currency, DateTime? ExpiresAt);
public sealed record QuoteDetailDto(Guid Id, string QuoteNumber, string Name, Guid? AccountId, Guid? OpportunityId, Guid? ContactId, string Status, DateTime? ExpiresAt, decimal Subtotal, decimal Discount, decimal Tax, decimal Total, string Currency, string? Notes, Guid? OwnerUserId, IReadOnlyList<QuoteLineDto> Lines);
public sealed record QuoteUpsertDto(Guid? Id, string QuoteNumber, string Name, Guid? AccountId, Guid? OpportunityId, Guid? ContactId, string Status, DateTime? ExpiresAt, decimal Subtotal, decimal Discount, decimal Tax, decimal Total, string Currency, string? Notes, Guid? OwnerUserId);
public sealed record QuoteLineDto(Guid Id, Guid QuoteId, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, decimal LineTotal, int SortOrder);
public sealed record QuoteLineUpsertDto(Guid? Id, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, int SortOrder);

public sealed record CampaignListItemDto(Guid Id, string Name, string Type, string Status, DateTime? StartDate, DateTime? EndDate);
public sealed record CampaignDetailDto(Guid Id, string Name, string Type, string Status, DateTime? StartDate, DateTime? EndDate, decimal? BudgetedCost, decimal? ActualCost, decimal? ExpectedRevenue, string? Description, Guid? OwnerUserId);
public sealed record CampaignUpsertDto(Guid? Id, string Name, string Type, string Status, DateTime? StartDate, DateTime? EndDate, decimal? BudgetedCost, decimal? ActualCost, decimal? ExpectedRevenue, string? Description, Guid? OwnerUserId);

public sealed record TicketListItemDto(Guid Id, string TicketNumber, string Subject, string Status, string Priority, string Type, Guid? AccountId, DateTime OpenedAt);
public sealed record TicketDetailDto(Guid Id, string TicketNumber, string Subject, string? Description, Guid? AccountId, Guid? ContactId, string Status, string Priority, string Type, string Channel, Guid? OwnerUserId, DateTime OpenedAt, DateTime? ResolvedAt, DateTime? ClosedAt);
public sealed record TicketUpsertDto(Guid? Id, string TicketNumber, string Subject, string? Description, Guid? AccountId, Guid? ContactId, string Status, string Priority, string Type, string Channel, Guid? OwnerUserId);

public sealed record DashboardSummaryDto(
    int OpenLeads,
    int OpenOpportunities,
    int WonOpportunitiesThisMonth,
    int OpenTickets,
    decimal TotalPipelineAmount,
    decimal WonAmountThisMonth,
    IReadOnlyList<OpportunityListItemDto> TopOpportunities,
    IReadOnlyList<ActivityListItemDto> RecentActivities);

public sealed record PipelineDto(Guid Id, string Name, string? Description, bool IsDefault, int SortOrder, IReadOnlyList<PipelineStageDto> Stages);
public sealed record PipelineStageDto(Guid Id, Guid PipelineId, string Name, int SortOrder, int Probability, bool IsWon, bool IsLost);
public sealed record PipelineUpsertDto(Guid? Id, string Name, string? Description, bool IsDefault, int SortOrder, IReadOnlyList<PipelineStageUpsertDto> Stages);
public sealed record PipelineStageUpsertDto(Guid? Id, string Name, int SortOrder, int Probability, bool IsWon, bool IsLost);

public sealed record TagDto(Guid Id, string Name, string? Color, string Scope);
public sealed record TagUpsertDto(Guid? Id, string Name, string? Color, string Scope);

public sealed record NoteDto(Guid Id, string Body, string RelatedType, Guid RelatedId, Guid? AuthorUserId, DateTime CreatedAt);
public sealed record NoteUpsertDto(Guid? Id, string Body, string RelatedType, Guid RelatedId, Guid? AuthorUserId);

public sealed record AttachmentDto(Guid Id, string FileName, string StoragePath, string? ContentType, long SizeBytes, string RelatedType, Guid RelatedId, Guid? UploadedByUserId, DateTime CreatedAt);
public sealed record AttachmentUpsertDto(Guid? Id, string FileName, string StoragePath, string? ContentType, long SizeBytes, string RelatedType, Guid RelatedId, Guid? UploadedByUserId);
