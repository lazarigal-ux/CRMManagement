namespace CRMManagement.Domain.Entities;

public class ZohoImportJob
{
    public Guid Id { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string Status { get; set; } = "Running";

    public string Modules { get; set; } = "";

    public int LeadsInserted { get; set; }
    public int LeadsUpdated  { get; set; }
    public int LeadsSkipped  { get; set; }
    public int LeadsErrored  { get; set; }

    public int ContactsInserted { get; set; }
    public int ContactsUpdated  { get; set; }
    public int ContactsSkipped  { get; set; }
    public int ContactsErrored  { get; set; }

    public int AccountsInserted { get; set; }
    public int AccountsUpdated  { get; set; }
    public int AccountsSkipped  { get; set; }
    public int AccountsErrored  { get; set; }

    public int DealsInserted { get; set; }
    public int DealsUpdated  { get; set; }
    public int DealsSkipped  { get; set; }
    public int DealsErrored  { get; set; }

    public int ProductsInserted { get; set; }
    public int ProductsUpdated  { get; set; }
    public int ProductsSkipped  { get; set; }
    public int ProductsErrored  { get; set; }

    public int QuotesInserted { get; set; }
    public int QuotesUpdated  { get; set; }
    public int QuotesSkipped  { get; set; }
    public int QuotesErrored  { get; set; }

    public int ActivitiesInserted { get; set; }
    public int ActivitiesUpdated  { get; set; }
    public int ActivitiesSkipped  { get; set; }
    public int ActivitiesErrored  { get; set; }

    public int CampaignsInserted { get; set; }
    public int CampaignsUpdated  { get; set; }
    public int CampaignsSkipped  { get; set; }
    public int CampaignsErrored  { get; set; }

    public int TicketsInserted { get; set; }
    public int TicketsUpdated  { get; set; }
    public int TicketsSkipped  { get; set; }
    public int TicketsErrored  { get; set; }

    public int InvoicesInserted { get; set; }
    public int InvoicesUpdated  { get; set; }
    public int InvoicesSkipped  { get; set; }
    public int InvoicesErrored  { get; set; }

    public int OrdersInserted { get; set; }
    public int OrdersUpdated  { get; set; }
    public int OrdersSkipped  { get; set; }
    public int OrdersErrored  { get; set; }

    public int NotesInserted { get; set; }
    public int NotesUpdated  { get; set; }
    public int NotesSkipped  { get; set; }
    public int NotesErrored  { get; set; }

    public int VendorsInserted { get; set; }
    public int VendorsUpdated  { get; set; }
    public int VendorsSkipped  { get; set; }
    public int VendorsErrored  { get; set; }

    public int PurchaseOrdersInserted { get; set; }
    public int PurchaseOrdersUpdated  { get; set; }
    public int PurchaseOrdersSkipped  { get; set; }
    public int PurchaseOrdersErrored  { get; set; }

    public int SolutionsInserted { get; set; }
    public int SolutionsUpdated  { get; set; }
    public int SolutionsSkipped  { get; set; }
    public int SolutionsErrored  { get; set; }

    public string? ErrorsJson { get; set; }
    public string? Message { get; set; }
}
