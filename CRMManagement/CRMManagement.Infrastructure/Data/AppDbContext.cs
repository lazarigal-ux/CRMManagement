using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CRMManagement.Domain.Entities;
using CRMManagement.Infrastructure.Identity;

namespace CRMManagement.Infrastructure.Data;

public sealed class AppDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,
    IdentityUserClaim<Guid>,
    IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>,
    IdentityRoleClaim<Guid>,
    IdentityUserToken<Guid>>
{
    public const string Schema = "crm";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<PriceBook> PriceBooks => Set<PriceBook>();
    public DbSet<PriceBookEntry> PriceBookEntries => Set<PriceBookEntry>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteLine> QuoteLines => Set<QuoteLine>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignMember> CampaignMembers => Set<CampaignMember>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<AccountTag> AccountTags => Set<AccountTag>();
    public DbSet<ContactTag> ContactTags => Set<ContactTag>();
    public DbSet<LeadTag> LeadTags => Set<LeadTag>();
    public DbSet<CustomField> CustomFields => Set<CustomField>();
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Territory> Territories => Set<Territory>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<UserRecentRecord> UserRecent => Set<UserRecentRecord>();
    public DbSet<UserStarredRecord> UserStarred => Set<UserStarredRecord>();
    public DbSet<AiExample> AiExamples => Set<AiExample>();
    public DbSet<AiInteractionLog> AiInteractionLogs => Set<AiInteractionLog>();
    public DbSet<CommunicationRecord> Communications => Set<CommunicationRecord>();
    public DbSet<ClassProductMapping> ClassProductMappings => Set<ClassProductMapping>();
    public DbSet<DrawingAnalysis> DrawingAnalyses => Set<DrawingAnalysis>();
    public DbSet<IntegrationOutboxEntry> IntegrationOutbox => Set<IntegrationOutboxEntry>();
    public DbSet<SavedView> SavedViews => Set<SavedView>();
    public DbSet<ZohoConnection> ZohoConnections => Set<ZohoConnection>();
    public DbSet<ZohoImportJob> ZohoImportJobs => Set<ZohoImportJob>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Solution> Solutions => Set<Solution>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);

        // Identity tables
        builder.Entity<ApplicationUser>().ToTable("crm_users", Schema);
        builder.Entity<ApplicationRole>().ToTable("crm_roles", Schema);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("crm_user_claims", Schema);
        builder.Entity<IdentityUserRole<Guid>>().ToTable("crm_user_roles", Schema);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("crm_user_logins", Schema);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("crm_role_claims", Schema);
        builder.Entity<IdentityUserToken<Guid>>().ToTable("crm_user_tokens", Schema);

        builder.Entity<Account>(b =>
        {
            b.ToTable("crm_accounts", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.LegalName).HasMaxLength(200);
            b.Property(x => x.Industry).HasMaxLength(120);
            b.Property(x => x.Website).HasMaxLength(300);
            b.Property(x => x.Phone).HasMaxLength(60);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.BillingAddress).HasMaxLength(500);
            b.Property(x => x.ShippingAddress).HasMaxLength(500);
            b.Property(x => x.AnnualRevenue).HasPrecision(18, 2);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.AccountType).HasMaxLength(80);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.Name).IsUnique();
            b.HasIndex(x => x.OwnerUserId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Contact>(b =>
        {
            b.ToTable("crm_contacts", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(120).IsRequired();
            b.Property(x => x.Title).HasMaxLength(150);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(60);
            b.Property(x => x.Mobile).HasMaxLength(60);
            b.Property(x => x.Department).HasMaxLength(150);
            b.Property(x => x.Address).HasMaxLength(500);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasOne(x => x.Account).WithMany(x => x.Contacts).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.Email);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Lead>(b =>
        {
            b.ToTable("crm_leads", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.FirstName).HasMaxLength(120).IsRequired();
            b.Property(x => x.LastName).HasMaxLength(120).IsRequired();
            b.Property(x => x.Company).HasMaxLength(200);
            b.Property(x => x.Title).HasMaxLength(150);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(60);
            b.Property(x => x.Source).HasMaxLength(80);
            b.Property(x => x.Status).HasMaxLength(40).IsRequired();
            b.Property(x => x.Rating).HasMaxLength(20);
            b.Property(x => x.Industry).HasMaxLength(120);
            b.Property(x => x.Website).HasMaxLength(300);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.Mobile).HasMaxLength(60);
            b.Property(x => x.AnnualRevenue).HasPrecision(18, 2);
            b.Property(x => x.Street).HasMaxLength(300);
            b.Property(x => x.City).HasMaxLength(120);
            b.Property(x => x.State).HasMaxLength(120);
            b.Property(x => x.ZipCode).HasMaxLength(40);
            b.Property(x => x.Country).HasMaxLength(120);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => new { x.Email, x.Company });
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Pipeline>(b =>
        {
            b.ToTable("crm_pipelines", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.HasIndex(x => x.Name).IsUnique();
            b.HasMany(x => x.Stages).WithOne(x => x.Pipeline).HasForeignKey(x => x.PipelineId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PipelineStage>(b =>
        {
            b.ToTable("crm_pipeline_stages", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(120).IsRequired();
            b.HasIndex(x => new { x.PipelineId, x.SortOrder });
        });

        builder.Entity<Opportunity>(b =>
        {
            b.ToTable("crm_opportunities", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.LeadSource).HasMaxLength(80);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.NextStep).HasMaxLength(500);
            b.Property(x => x.Type).HasMaxLength(60);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasOne(x => x.Account).WithMany(x => x.Opportunities).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Pipeline).WithMany().HasForeignKey(x => x.PipelineId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Activity>(b =>
        {
            b.ToTable("crm_activities", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(40).IsRequired();
            b.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.Priority).HasMaxLength(30);
            b.Property(x => x.RelatedType).HasMaxLength(40);
            b.Property(x => x.Location).HasMaxLength(300);
            b.Property(x => x.CallType).HasMaxLength(40);
            b.Property(x => x.CallDurationSeconds).HasMaxLength(20);
            b.Property(x => x.ActivityType).HasMaxLength(80);
            b.Property(x => x.EventTitle).HasMaxLength(300);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => new { x.RelatedType, x.RelatedId });
            b.HasIndex(x => x.OwnerUserId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Product>(b =>
        {
            b.ToTable("crm_products", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Sku).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.Family).HasMaxLength(120);
            b.Property(x => x.Unit).HasMaxLength(40);
            b.Property(x => x.StandardPrice).HasPrecision(18, 2);
            b.Property(x => x.Cost).HasPrecision(18, 2);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.Sku).IsUnique();
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<PriceBook>(b =>
        {
            b.ToTable("crm_price_books", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<PriceBookEntry>(b =>
        {
            b.ToTable("crm_price_book_entries", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.HasIndex(x => new { x.PriceBookId, x.ProductId }).IsUnique();
        });

        builder.Entity<Quote>(b =>
        {
            b.ToTable("crm_quotes", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.QuoteNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.Subtotal).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.Tax).HasPrecision(18, 2);
            b.Property(x => x.Total).HasPrecision(18, 2);
            b.Property(x => x.AcceptedByName).HasMaxLength(200);
            b.Property(x => x.AcceptedByEmail).HasMaxLength(200);
            b.Property(x => x.AcceptedFromIp).HasMaxLength(64);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.QuoteNumber).IsUnique();
            b.HasIndex(x => x.SignatureToken).IsUnique()
                .HasFilter("\"SignatureToken\" is not null");
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
            b.HasMany(x => x.Lines).WithOne(x => x.Quote).HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuoteLine>(b =>
        {
            b.ToTable("crm_quote_lines", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.QuoteId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Order>(b =>
        {
            b.ToTable("crm_orders", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.Subtotal).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.Tax).HasPrecision(18, 2);
            b.Property(x => x.Total).HasPrecision(18, 2);
            b.Property(x => x.BillingAddress).HasMaxLength(500);
            b.Property(x => x.ShippingAddress).HasMaxLength(500);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
            b.HasMany(x => x.Lines).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OrderLine>(b =>
        {
            b.ToTable("crm_order_lines", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.OrderId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Invoice>(b =>
        {
            b.ToTable("crm_invoices", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.InvoiceNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(4000);
            b.Property(x => x.Subtotal).HasPrecision(18, 2);
            b.Property(x => x.Tax).HasPrecision(18, 2);
            b.Property(x => x.Total).HasPrecision(18, 2);
            b.Property(x => x.AmountPaid).HasPrecision(18, 2);
            b.Property(x => x.BillingAddress).HasMaxLength(500);
            b.Property(x => x.ShippingAddress).HasMaxLength(500);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.InvoiceNumber).IsUnique();
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
            b.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvoiceLine>(b =>
        {
            b.ToTable("crm_invoice_lines", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.InvoiceId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Campaign>(b =>
        {
            b.ToTable("crm_campaigns", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Type).HasMaxLength(40).IsRequired();
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.BudgetedCost).HasPrecision(18, 2);
            b.Property(x => x.ActualCost).HasPrecision(18, 2);
            b.Property(x => x.ExpectedRevenue).HasPrecision(18, 2);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
            b.HasMany(x => x.Members).WithOne(x => x.Campaign).HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CampaignMember>(b =>
        {
            b.ToTable("crm_campaign_members", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.HasIndex(x => new { x.CampaignId, x.LeadId });
            b.HasIndex(x => new { x.CampaignId, x.ContactId });
        });

        builder.Entity<Ticket>(b =>
        {
            b.ToTable("crm_tickets", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.TicketNumber).HasMaxLength(40).IsRequired();
            b.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.Status).HasMaxLength(30).IsRequired();
            b.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            b.Property(x => x.Type).HasMaxLength(30).IsRequired();
            b.Property(x => x.Channel).HasMaxLength(30).IsRequired();
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.TicketNumber).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
            b.HasMany(x => x.Comments).WithOne(x => x.Ticket).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TicketComment>(b =>
        {
            b.ToTable("crm_ticket_comments", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Body).IsRequired();
            b.HasIndex(x => x.TicketId);
        });

        builder.Entity<Note>(b =>
        {
            b.ToTable("crm_notes", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Body).IsRequired();
            b.Property(x => x.RelatedType).HasMaxLength(40).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => new { x.RelatedType, x.RelatedId });
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Attachment>(b =>
        {
            b.ToTable("crm_attachments", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            b.Property(x => x.StoragePath).HasMaxLength(500).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(200);
            b.Property(x => x.RelatedType).HasMaxLength(40).IsRequired();
            b.HasIndex(x => new { x.RelatedType, x.RelatedId });
        });

        builder.Entity<Tag>(b =>
        {
            b.ToTable("crm_tags", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(80).IsRequired();
            b.Property(x => x.Color).HasMaxLength(20);
            b.Property(x => x.Scope).HasMaxLength(30).IsRequired();
            b.HasIndex(x => new { x.Name, x.Scope }).IsUnique();
        });

        builder.Entity<AccountTag>(b =>
        {
            b.ToTable("crm_account_tags", Schema);
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TargetId, x.TagId }).IsUnique();
        });

        builder.Entity<ContactTag>(b =>
        {
            b.ToTable("crm_contact_tags", Schema);
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TargetId, x.TagId }).IsUnique();
        });

        builder.Entity<LeadTag>(b =>
        {
            b.ToTable("crm_lead_tags", Schema);
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TargetId, x.TagId }).IsUnique();
        });

        builder.Entity<CustomField>(b =>
        {
            b.ToTable("crm_custom_fields", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            b.Property(x => x.Name).HasMaxLength(80).IsRequired();
            b.Property(x => x.Label).HasMaxLength(150).IsRequired();
            b.Property(x => x.DataType).HasMaxLength(30).IsRequired();
            b.HasIndex(x => new { x.EntityType, x.Name }).IsUnique();
        });

        builder.Entity<CustomFieldValue>(b =>
        {
            b.ToTable("crm_custom_field_values", Schema);
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CustomFieldId, x.EntityId });
        });

        builder.Entity<Team>(b =>
        {
            b.ToTable("crm_teams", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
        });

        builder.Entity<TeamMember>(b =>
        {
            b.ToTable("crm_team_members", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Role).HasMaxLength(60);
            b.HasIndex(x => new { x.TeamId, x.UserId }).IsUnique();
        });

        builder.Entity<Territory>(b =>
        {
            b.ToTable("crm_territories", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
        });

        builder.Entity<EmailTemplate>(b =>
        {
            b.ToTable("crm_email_templates", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            b.Property(x => x.Body).IsRequired();
            b.Property(x => x.Language).HasMaxLength(10);
            b.Property(x => x.Category).HasMaxLength(60);
            b.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<AuditLogEntry>(b =>
        {
            b.ToTable("crm_audit_log", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.EntityType).HasMaxLength(60).IsRequired();
            b.Property(x => x.Action).HasMaxLength(20).IsRequired();
            b.HasIndex(x => new { x.EntityType, x.EntityId });
            b.HasIndex(x => x.At);
        });

        builder.Entity<UserRecentRecord>(b =>
        {
            b.ToTable("crm_user_recent", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            b.HasIndex(x => new { x.UserId, x.EntityType, x.EntityId }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.LastVisitedAt });
        });

        builder.Entity<UserStarredRecord>(b =>
        {
            b.ToTable("crm_user_starred", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            b.HasIndex(x => new { x.UserId, x.EntityType, x.EntityId }).IsUnique();
            b.HasIndex(x => new { x.UserId, x.StarredAt });
        });

        builder.Entity<AiExample>(b =>
        {
            b.ToTable("crm_ai_examples", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Category).HasMaxLength(100).IsRequired();
            b.Property(x => x.Tags).HasColumnType("text[]");
            b.Property(x => x.Instruction).IsRequired();
            b.Property(x => x.Provider).HasMaxLength(40);
            b.Property(x => x.Rating).HasDefaultValue((short)0);
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.Tags).HasMethod("gin");
            b.HasIndex(x => x.Rating);
        });

        builder.Entity<AiInteractionLog>(b =>
        {
            b.ToTable("crm_ai_interaction_log", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Instruction).IsRequired();
            b.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            b.Property(x => x.Mode).HasMaxLength(40);
            b.Property(x => x.ErrorMessage).HasMaxLength(2000);
            b.Property(x => x.SourceFileName).HasMaxLength(300);
            b.Property(x => x.Feedback).HasDefaultValue((short)0);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.Provider);
            b.HasIndex(x => x.IsFinalStep);
        });

        builder.Entity<CommunicationRecord>(b =>
        {
            b.ToTable("crm_communications", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Provider).HasMaxLength(40).IsRequired();
            b.Property(x => x.Direction).HasMaxLength(10).IsRequired();
            b.Property(x => x.FromAddress).HasMaxLength(300);
            b.Property(x => x.ToAddress).HasMaxLength(300);
            b.Property(x => x.Subject).HasMaxLength(500);
            b.Property(x => x.ExternalId).HasMaxLength(200);
            b.HasIndex(x => x.OccurredAt);
            b.HasIndex(x => x.ContactId);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.OpportunityId);
            b.HasIndex(x => x.LeadId);
            b.HasIndex(x => new { x.Provider, x.ExternalId }).IsUnique()
                .HasFilter("\"ExternalId\" is not null");
        });

        builder.Entity<ClassProductMapping>(b =>
        {
            b.ToTable("crm_class_product_mappings", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Label).HasMaxLength(120).IsRequired();
            b.Property(x => x.Multiplier).HasPrecision(10, 3);
            b.Property(x => x.Notes).HasMaxLength(500);
            b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.Label).IsUnique();
        });

        builder.Entity<DrawingAnalysis>(b =>
        {
            b.ToTable("crm_drawing_analyses", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.SourceFileName).HasMaxLength(300).IsRequired();
            b.Property(x => x.MediaType).HasMaxLength(80).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(2000);
            b.HasIndex(x => x.OpportunityId);
            b.HasIndex(x => x.AccountId);
            b.HasIndex(x => x.Status);
        });

        builder.Entity<IntegrationOutboxEntry>(b =>
        {
            b.ToTable("crm_integration_outbox", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Target).HasMaxLength(80).IsRequired();
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.LastError).HasMaxLength(2000);
            b.Property(x => x.RelatedType).HasMaxLength(40);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Target);
            b.HasIndex(x => new { x.RelatedType, x.RelatedId });
        });

        builder.Entity<SavedView>(b =>
        {
            b.ToTable("crm_saved_views", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
            b.Property(x => x.Name).HasMaxLength(150).IsRequired();
            b.Property(x => x.ViewMode).HasMaxLength(20).IsRequired();
            b.Property(x => x.FiltersJson).IsRequired();
            b.HasIndex(x => new { x.EntityType, x.OwnerUserId });
            b.HasIndex(x => new { x.EntityType, x.IsShared });
        });

        builder.Entity<ZohoConnection>(b =>
        {
            b.ToTable("crm_zoho_connection", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Region).HasMaxLength(20).IsRequired();
            b.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            b.Property(x => x.ClientSecretProtected).IsRequired();
            b.Property(x => x.AccountOwnerEmail).HasMaxLength(200);
            b.Property(x => x.AccountOwnerName).HasMaxLength(200);
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.LastError).HasMaxLength(2000);
            b.Property(x => x.AccessTokenProtected);
            b.Property(x => x.AccessTokenExpiresAt);
            b.HasIndex(x => x.CompanyId).IsUnique().HasFilter("\"CompanyId\" is not null");
        });

        builder.Entity<ZohoImportJob>(b =>
        {
            b.ToTable("crm_zoho_import_job", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.Modules).HasMaxLength(200).IsRequired();
            b.Property(x => x.Message).HasMaxLength(2000);
            b.HasIndex(x => x.StartedAt);
            b.HasIndex(x => x.Status);
        });

        builder.Entity<Vendor>(b =>
        {
            b.ToTable("crm_vendors", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Category).HasMaxLength(120);
            b.Property(x => x.Email).HasMaxLength(200);
            b.Property(x => x.Phone).HasMaxLength(60);
            b.Property(x => x.Website).HasMaxLength(300);
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.Street).HasMaxLength(300);
            b.Property(x => x.City).HasMaxLength(120);
            b.Property(x => x.State).HasMaxLength(120);
            b.Property(x => x.ZipCode).HasMaxLength(40);
            b.Property(x => x.Country).HasMaxLength(120);
            b.Property(x => x.GlAccount).HasMaxLength(120);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.OwnerUserId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("crm_purchase_orders", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.PoNumber).HasMaxLength(60).IsRequired();
            b.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            b.Property(x => x.Status).HasMaxLength(40).IsRequired();
            b.Property(x => x.CarrierName).HasMaxLength(120);
            b.Property(x => x.Subtotal).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.Tax).HasPrecision(18, 2);
            b.Property(x => x.AdjustmentAmount).HasPrecision(18, 2);
            b.Property(x => x.Total).HasPrecision(18, 2);
            b.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            b.Property(x => x.Description).HasMaxLength(4000);
            b.Property(x => x.TermsAndConditions).HasMaxLength(4000);
            b.Property(x => x.BillingAddress).HasMaxLength(500);
            b.Property(x => x.ShippingAddress).HasMaxLength(500);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasOne(x => x.Vendor).WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.PoNumber).IsUnique();
            b.HasIndex(x => x.VendorId);
            b.HasIndex(x => x.OwnerUserId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
            b.HasMany(x => x.Lines).WithOne(x => x.PurchaseOrder).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PurchaseOrderLine>(b =>
        {
            b.ToTable("crm_purchase_order_lines", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.UnitPrice).HasPrecision(18, 2);
            b.Property(x => x.Discount).HasPrecision(18, 2);
            b.Property(x => x.LineTotal).HasPrecision(18, 2);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.PurchaseOrderId);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        builder.Entity<Solution>(b =>
        {
            b.ToTable("crm_solutions", Schema);
            b.HasKey(x => x.Id);
            b.Property(x => x.SolutionNumber).HasMaxLength(60).IsRequired();
            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Category).HasMaxLength(120);
            b.Property(x => x.Status).HasMaxLength(40).IsRequired();
            b.Property(x => x.Question).HasMaxLength(4000);
            b.Property(x => x.Answer).HasMaxLength(8000);
            b.Property(x => x.Comments).HasMaxLength(4000);
            b.Property(x => x.ZohoId).HasMaxLength(60);
            b.HasIndex(x => x.Title);
            b.HasIndex(x => x.ZohoId).IsUnique().HasFilter("\"ZohoId\" is not null");
        });

        // Auditable timestamps default values
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType == null) continue;
            if (typeof(AuditableEntity).IsAssignableFrom(clrType))
            {
                builder.Entity(clrType).Property<DateTime>(nameof(AuditableEntity.CreatedAt)).HasDefaultValueSql("now() at time zone 'utc'");
                builder.Entity(clrType).Property<DateTime>(nameof(AuditableEntity.UpdatedAt)).HasDefaultValueSql("now() at time zone 'utc'");
            }
        }

        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
            b.Property(x => x.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = utcNow;
                    auditable.UpdatedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = utcNow;
                }
            }

            if (entry.Entity is ApplicationUser user)
            {
                if (entry.State == EntityState.Added)
                {
                    user.CreatedAt = utcNow;
                    user.UpdatedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    user.UpdatedAt = utcNow;
                }
            }

            if (entry.Entity is ApplicationRole role)
            {
                if (entry.State == EntityState.Added)
                {
                    role.CreatedAt = utcNow;
                    role.UpdatedAt = utcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    role.UpdatedAt = utcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
