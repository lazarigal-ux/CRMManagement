namespace CRMManagement.Application.DTOs;

public sealed record LeadListItemDto(Guid Id, string FirstName, string LastName, string? Company, string? Email, string Status, string? Rating, int Score, DateTime CreatedAt)
{
    // Extra Zoho fields surfaced via the column-picker.
    public string? Mobile { get; init; }
    public string? Title { get; init; }
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public string? Source { get; init; }
    public string? Description { get; init; }
    public decimal? AnnualRevenue { get; init; }
    public int? NoOfEmployees { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record LeadDetailDto(Guid Id, string FirstName, string LastName, string? Company, string? Title, string? Email, string? Phone, string? Mobile, string? Source, string Status, string? Rating, int Score, Guid? OwnerUserId, string? Industry, string? Website, string? Description, decimal? AnnualRevenue, int? NoOfEmployees, string? Street, string? City, string? State, string? ZipCode, string? Country, Guid? ConvertedAccountId, Guid? ConvertedContactId, Guid? ConvertedOpportunityId, DateTime? ConvertedAt);
public sealed record LeadUpsertDto(Guid? Id, string FirstName, string LastName, string? Company, string? Title, string? Email, string? Phone, string? Mobile, string? Source, string Status, string? Rating, int Score, Guid? OwnerUserId, string? Industry, string? Website, string? Description, decimal? AnnualRevenue, int? NoOfEmployees, string? Street, string? City, string? State, string? ZipCode, string? Country);
public sealed record LeadConversionResultDto(Guid LeadId, Guid AccountId, Guid ContactId, Guid OpportunityId);

public sealed record AccountListItemDto(Guid Id, string Name, string? Industry, string? Website, string? Phone, bool IsActive)
{
    public string? LegalName { get; init; }
    public string? Email { get; init; }
    public string? AccountType { get; init; }
    public decimal? AnnualRevenue { get; init; }
    public int? EmployeeCount { get; init; }
    public string? BillingAddress { get; init; }
    public string? ShippingAddress { get; init; }
    public string? Description { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record AccountDetailDto(Guid Id, string Name, string? LegalName, string? Industry, string? Website, string? Phone, string? Email, string? BillingAddress, string? ShippingAddress, decimal? AnnualRevenue, int? EmployeeCount, Guid? OwnerUserId, Guid? ParentAccountId, string? Description, bool IsActive);
public sealed record AccountUpsertDto(Guid? Id, string Name, string? LegalName, string? Industry, string? Website, string? Phone, string? Email, string? BillingAddress, string? ShippingAddress, decimal? AnnualRevenue, int? EmployeeCount, Guid? OwnerUserId, Guid? ParentAccountId, string? Description, bool IsActive);

public sealed record ContactListItemDto(Guid Id, string FirstName, string LastName, string? Email, string? Phone, Guid? AccountId)
{
    public string? Title { get; init; }
    public string? Mobile { get; init; }
    public string? Department { get; init; }
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string? AccountName { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record ContactDetailDto(Guid Id, string FirstName, string LastName, string? Title, string? Email, string? Phone, string? Mobile, Guid? AccountId, Guid? OwnerUserId, string? Department, string? Address, string? Description, bool IsPrimary, bool DoNotContact);
public sealed record ContactUpsertDto(Guid? Id, string FirstName, string LastName, string? Title, string? Email, string? Phone, string? Mobile, Guid? AccountId, Guid? OwnerUserId, string? Department, string? Address, string? Description, bool IsPrimary, bool DoNotContact);

public sealed record OpportunityListItemDto(Guid Id, string Name, Guid? AccountId, decimal Amount, string Currency, Guid StageId, string Status, DateTime? CloseDate)
{
    public int? Probability { get; init; }
    public string? LeadSource { get; init; }
    public string? Description { get; init; }
    public string? NextStep { get; init; }
    public string? Type { get; init; }
    public string? StageName { get; init; }
    public string? AccountName { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record OpportunityDetailDto(Guid Id, string Name, Guid? AccountId, Guid? ContactId, Guid PipelineId, Guid StageId, decimal Amount, string Currency, DateTime? CloseDate, int Probability, string Status, string? LeadSource, Guid? OwnerUserId, string? Description, string? NextStep);
public sealed record OpportunityUpsertDto(Guid? Id, string Name, Guid? AccountId, Guid? ContactId, Guid PipelineId, Guid StageId, decimal Amount, string Currency, DateTime? CloseDate, int Probability, string Status, string? LeadSource, Guid? OwnerUserId, string? Description, string? NextStep);

public sealed record ActivityListItemDto(Guid Id, string Type, string Subject, string Status, DateTime? DueDate, Guid? OwnerUserId, string? RelatedType, Guid? RelatedId)
{
    // Extra fields exposed for the column-picker UI; default values keep
    // existing positional callers working without changes.
    public string? Priority { get; init; }
    public string? Description { get; init; }
    public DateTime? StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public string? Location { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ActivityType { get; init; }
    public string? CallType { get; init; }
    public string? CallDurationSeconds { get; init; }
    public string? EventTitle { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
    public string? OwnerName { get; init; }
}
public sealed record ActivityDetailDto(Guid Id, string Type, string Subject, string? Description, DateTime? StartAt, DateTime? EndAt, DateTime? DueDate, string Status, string? Priority, Guid? OwnerUserId, string? RelatedType, Guid? RelatedId, string? Location, DateTime? CompletedAt);
public sealed record ActivityUpsertDto(Guid? Id, string Type, string Subject, string? Description, DateTime? StartAt, DateTime? EndAt, DateTime? DueDate, string Status, string? Priority, Guid? OwnerUserId, string? RelatedType, Guid? RelatedId, string? Location);

public sealed record ProductListItemDto(Guid Id, string Sku, string Name, string? Family, bool IsActive, decimal StandardPrice)
{
    public string? Description { get; init; }
    public decimal? Cost { get; init; }
    public string? Unit { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record ProductDetailDto(Guid Id, string Sku, string Name, string? Description, string? Family, bool IsActive, decimal StandardPrice, decimal? Cost, string? Unit);
public sealed record ProductUpsertDto(Guid? Id, string Sku, string Name, string? Description, string? Family, bool IsActive, decimal StandardPrice, decimal? Cost, string? Unit);

public sealed record QuoteListItemDto(Guid Id, string QuoteNumber, string Name, Guid? AccountId, string Status, decimal Total, string Currency, DateTime? ExpiresAt)
{
    public string? AccountName { get; init; }
    public string? OpportunityName { get; init; }
    public string? ContactName { get; init; }
    public decimal? Subtotal { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Tax { get; init; }
    public string? Notes { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public string? AcceptedByName { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record QuoteDetailDto(Guid Id, string QuoteNumber, string Name, Guid? AccountId, Guid? OpportunityId, Guid? ContactId, string Status, DateTime? ExpiresAt, decimal Subtotal, decimal Discount, decimal Tax, decimal Total, string Currency, string? Notes, Guid? OwnerUserId, IReadOnlyList<QuoteLineDto> Lines);
public sealed record QuoteUpsertDto(Guid? Id, string QuoteNumber, string Name, Guid? AccountId, Guid? OpportunityId, Guid? ContactId, string Status, DateTime? ExpiresAt, decimal Subtotal, decimal Discount, decimal Tax, decimal Total, string Currency, string? Notes, Guid? OwnerUserId);
public sealed record QuoteLineDto(Guid Id, Guid QuoteId, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, decimal LineTotal, int SortOrder);
public sealed record QuoteLineUpsertDto(Guid? Id, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, int SortOrder);

public sealed record CampaignListItemDto(Guid Id, string Name, string Type, string Status, DateTime? StartDate, DateTime? EndDate)
{
    public decimal? BudgetedCost { get; init; }
    public decimal? ActualCost { get; init; }
    public decimal? ExpectedRevenue { get; init; }
    public string? Description { get; init; }
    public int? NumSent { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record CampaignDetailDto(Guid Id, string Name, string Type, string Status, DateTime? StartDate, DateTime? EndDate, decimal? BudgetedCost, decimal? ActualCost, decimal? ExpectedRevenue, string? Description, Guid? OwnerUserId);
public sealed record CampaignUpsertDto(Guid? Id, string Name, string Type, string Status, DateTime? StartDate, DateTime? EndDate, decimal? BudgetedCost, decimal? ActualCost, decimal? ExpectedRevenue, string? Description, Guid? OwnerUserId);

public sealed record TicketListItemDto(Guid Id, string TicketNumber, string Subject, string Status, string Priority, string Type, Guid? AccountId, DateTime OpenedAt)
{
    public string? AccountName { get; init; }
    public string? ContactName { get; init; }
    public string? Channel { get; init; }
    public string? ReportedBy { get; init; }
    public string? Description { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record TicketDetailDto(Guid Id, string TicketNumber, string Subject, string? Description, Guid? AccountId, Guid? ContactId, string Status, string Priority, string Type, string Channel, string? ReportedBy, Guid? OwnerUserId, DateTime OpenedAt, DateTime? ResolvedAt, DateTime? ClosedAt);
public sealed record TicketUpsertDto(Guid? Id, string TicketNumber, string Subject, string? Description, Guid? AccountId, Guid? ContactId, string Status, string Priority, string Type, string Channel, string? ReportedBy, Guid? OwnerUserId);

public sealed record MeetingListItemDto(Guid Id, string Subject, DateTime? StartAt, DateTime? EndAt, string? Location, string Status, string? RelatedType, Guid? RelatedId, string? AccountName)
{
    public string? OwnerName { get; init; }
    public string? Description { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record DashboardTaskDto(Guid Id, string Subject, DateTime? DueDate, string Status, string? Priority, string? RelatedType, Guid? RelatedId, string? AccountName)
{
    // Extra fields surfaced via the column-picker on the home dashboard widget.
    public string? Description { get; init; }
    public DateTime? StartAt { get; init; }
    public DateTime? EndAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? Location { get; init; }
    public string? ActivityType { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record DashboardLeadDto(Guid Id, string FirstName, string LastName, string? Company, string? Email, string? Phone, string? Source, string Status, DateTime CreatedAt)
{
    public string? Title { get; init; }
    public string? Mobile { get; init; }
    public string? Industry { get; init; }
    public string? Website { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
}
public sealed record PipelineStageTotalDto(string StageName, int Count, decimal Total);

public sealed record DashboardSummaryDto(
    string WelcomeName,
    int OpenLeads,
    int OpenOpportunities,
    int WonOpportunitiesThisMonth,
    int OpenTickets,
    decimal TotalPipelineAmount,
    decimal WonAmountThisMonth,
    IReadOnlyList<OpportunityListItemDto> TopOpportunities,
    IReadOnlyList<ActivityListItemDto> RecentActivities,
    int OpenLeadsPrev,
    int WonOpportunitiesPrevMonth,
    decimal WonAmountPrevMonth,
    decimal MonthlyRevenueTarget,
    IReadOnlyList<OpportunityListItemDto> ClosingThisMonth,
    int MyOpenDeals,
    int MyUntouchedDeals,
    int MyCallsToday,
    int MyLeads,
    IReadOnlyList<DashboardTaskDto> MyOpenTasks,
    IReadOnlyList<MeetingListItemDto> MyMeetings,
    IReadOnlyList<DashboardLeadDto> TodaysLeads,
    IReadOnlyList<PipelineStageTotalDto> MyPipelineByStage);

public sealed record PipelineDto(Guid Id, string Name, string? Description, bool IsDefault, int SortOrder, IReadOnlyList<PipelineStageDto> Stages);
public sealed record PipelineStageDto(Guid Id, Guid PipelineId, string Name, int SortOrder, int Probability, bool IsWon, bool IsLost);
public sealed record PipelineUpsertDto(Guid? Id, string Name, string? Description, bool IsDefault, int SortOrder, IReadOnlyList<PipelineStageUpsertDto> Stages);
public sealed record PipelineStageUpsertDto(Guid? Id, string Name, int SortOrder, int Probability, bool IsWon, bool IsLost);

public sealed record TagDto(Guid Id, string Name, string? Color, string Scope);
public sealed record TagUpsertDto(Guid? Id, string Name, string? Color, string Scope);

public sealed record NoteDto(Guid Id, string? Title, string Body, string RelatedType, Guid RelatedId, Guid? AuthorUserId, DateTime CreatedAt)
{
    public string? AuthorName { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record NoteUpsertDto(Guid? Id, string? Title, string Body, string RelatedType, Guid RelatedId, Guid? AuthorUserId);

public sealed record AttachmentDto(Guid Id, string FileName, string StoragePath, string? ContentType, long SizeBytes, string RelatedType, Guid RelatedId, Guid? UploadedByUserId, DateTime CreatedAt);
public sealed record AttachmentUpsertDto(Guid? Id, string FileName, string StoragePath, string? ContentType, long SizeBytes, string RelatedType, Guid RelatedId, Guid? UploadedByUserId);

public sealed record SavedViewDto(Guid Id, string EntityType, string Name, Guid? OwnerUserId, string ViewMode, string FiltersJson, string? ColumnsJson, bool IsShared, bool IsDefault);
public sealed record SavedViewUpsertDto(Guid? Id, string EntityType, string Name, Guid? OwnerUserId, string ViewMode, string FiltersJson, string? ColumnsJson, bool IsShared, bool IsDefault);

public sealed record VendorListItemDto(Guid Id, string Name, string? Category, string? Email, string? Phone, string? Website, bool IsActive)
{
    public string? Description { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? GlAccount { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record VendorDetailDto(Guid Id, string Name, string? Category, string? Email, string? Phone, string? Website, string? Description, string? Street, string? City, string? State, string? ZipCode, string? Country, string? GlAccount, Guid? OwnerUserId, bool IsActive);
public sealed record VendorUpsertDto(Guid? Id, string Name, string? Category, string? Email, string? Phone, string? Website, string? Description, string? Street, string? City, string? State, string? ZipCode, string? Country, string? GlAccount, Guid? OwnerUserId, bool IsActive);

public sealed record PurchaseOrderListItemDto(Guid Id, string PoNumber, string Subject, Guid? VendorId, string Status, decimal Total, string Currency, DateTime? PoDate, DateTime? DueDate)
{
    public string? VendorName { get; init; }
    public string? RequisitionNo { get; init; }
    public string? CarrierName { get; init; }
    public decimal? Subtotal { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Tax { get; init; }
    public decimal? AdjustmentAmount { get; init; }
    public string? Description { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record PurchaseOrderDetailDto(Guid Id, string PoNumber, string Subject, string? RequisitionNo, Guid? VendorId, string Status, DateTime? PoDate, DateTime? DueDate, string? CarrierName, decimal Subtotal, decimal Discount, decimal Tax, decimal AdjustmentAmount, decimal Total, string Currency, string? Description, string? TermsAndConditions, string? BillingAddress, string? ShippingAddress, Guid? OwnerUserId, IReadOnlyList<PurchaseOrderLineDto> Lines);
public sealed record PurchaseOrderUpsertDto(Guid? Id, string PoNumber, string Subject, string? RequisitionNo, Guid? VendorId, string Status, DateTime? PoDate, DateTime? DueDate, string? CarrierName, decimal Subtotal, decimal Discount, decimal Tax, decimal AdjustmentAmount, decimal Total, string Currency, string? Description, string? TermsAndConditions, string? BillingAddress, string? ShippingAddress, Guid? OwnerUserId);
public sealed record PurchaseOrderLineDto(Guid Id, Guid PurchaseOrderId, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, decimal LineTotal, int SortOrder);
public sealed record PurchaseOrderLineUpsertDto(Guid? Id, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, int SortOrder);

public sealed record InvoiceLineDto(Guid Id, Guid InvoiceId, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal LineTotal, int SortOrder);
public sealed record InvoiceListItemDto(Guid Id, string InvoiceNumber, string? Subject, Guid AccountId, string Status, decimal Total, decimal AmountPaid, string Currency, DateTime IssueDate, DateTime? DueDate)
{
    public string? AccountName { get; init; }
    public string? OrderNumber { get; init; }
    public DateTime? PaidAt { get; init; }
    public decimal? Subtotal { get; init; }
    public decimal? Tax { get; init; }
    public string? Notes { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record InvoiceDetailDto(Guid Id, string InvoiceNumber, string? Subject, Guid AccountId, Guid? OrderId, string Status, DateTime IssueDate, DateTime? DueDate, DateTime? PaidAt, decimal Subtotal, decimal Tax, decimal Total, decimal AmountPaid, string Currency, string? Notes, string? BillingAddress, string? ShippingAddress, IReadOnlyList<InvoiceLineDto> Lines);

public sealed record OrderLineDto(Guid Id, Guid OrderId, Guid? ProductId, string? Description, decimal Quantity, decimal UnitPrice, decimal Discount, decimal LineTotal, int SortOrder);
public sealed record OrderListItemDto(Guid Id, string OrderNumber, string? Subject, Guid AccountId, string Status, decimal Total, string Currency, DateTime OrderDate)
{
    public string? AccountName { get; init; }
    public string? OpportunityName { get; init; }
    public string? QuoteNumber { get; init; }
    public decimal? Subtotal { get; init; }
    public decimal? Discount { get; init; }
    public decimal? Tax { get; init; }
    public string? Notes { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record OrderDetailDto(Guid Id, string OrderNumber, string? Subject, Guid AccountId, Guid? OpportunityId, Guid? QuoteId, string Status, DateTime OrderDate, decimal Subtotal, decimal Discount, decimal Tax, decimal Total, string Currency, string? Notes, Guid? OwnerUserId, string? BillingAddress, string? ShippingAddress, IReadOnlyList<OrderLineDto> Lines);

public sealed record SolutionListItemDto(Guid Id, string SolutionNumber, string Title, string? Category, string Status, bool Published)
{
    public string? Question { get; init; }
    public string? Answer { get; init; }
    public string? ProductName { get; init; }
    public string? Comments { get; init; }
    public string? OwnerName { get; init; }
    public DateTime? ZohoCreatedTime { get; init; }
    public DateTime? ZohoModifiedTime { get; init; }
}
public sealed record SolutionDetailDto(Guid Id, string SolutionNumber, string Title, string? Question, string? Answer, string? Category, string Status, Guid? ProductId, bool Published, string? Comments, Guid? OwnerUserId);
public sealed record SolutionUpsertDto(Guid? Id, string SolutionNumber, string Title, string? Question, string? Answer, string? Category, string Status, Guid? ProductId, bool Published, string? Comments, Guid? OwnerUserId);
