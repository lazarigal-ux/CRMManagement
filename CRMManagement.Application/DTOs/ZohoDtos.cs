using System.Text.Json.Serialization;

namespace CRMManagement.Application.DTOs;

public sealed class ZohoPage<T>
{
    [JsonPropertyName("data")] public IReadOnlyList<T> Data { get; init; } = Array.Empty<T>();
    [JsonPropertyName("info")] public ZohoPageInfo Info { get; init; } = new();
}

/// <summary>A page that exposes the raw JsonElement for each record alongside the typed DTO,
/// so callers can extract custom-field values not bound to standard DTO properties.</summary>
public sealed class ZohoRawPage<T> where T : class
{
    public IReadOnlyList<(T Dto, System.Text.Json.JsonElement Raw)> Items { get; init; }
        = Array.Empty<(T, System.Text.Json.JsonElement)>();
    public ZohoPageInfo Info { get; init; } = new();
}

public sealed class ZohoPageInfo
{
    [JsonPropertyName("per_page")]     public int  PerPage     { get; init; }
    [JsonPropertyName("count")]        public int  Count       { get; init; }
    [JsonPropertyName("page")]         public int  Page        { get; init; }
    [JsonPropertyName("more_records")] public bool MoreRecords { get; init; }
}

public sealed class ZohoOwnerDto
{
    [JsonPropertyName("id")]    public string? Id    { get; init; }
    [JsonPropertyName("name")]  public string? Name  { get; init; }
    [JsonPropertyName("email")] public string? Email { get; init; }
}

public sealed class ZohoAccountRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoContactRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoLeadDto
{
    public const string Fields =
        "id,First_Name,Last_Name,Full_Name,Email,Phone,Mobile,Title,Company,Industry,Website," +
        "Lead_Status,Lead_Source,Rating,Description,Street,City,State,Zip_Code,Country," +
        "Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]            public string  Id           { get; init; } = "";
    [JsonPropertyName("First_Name")]    public string? FirstName    { get; init; }
    [JsonPropertyName("Last_Name")]     public string? LastName     { get; init; }
    [JsonPropertyName("Full_Name")]     public string? FullName     { get; init; }
    [JsonPropertyName("Email")]         public string? Email        { get; init; }
    [JsonPropertyName("Phone")]         public string? Phone        { get; init; }
    [JsonPropertyName("Mobile")]        public string? Mobile       { get; init; }
    [JsonPropertyName("Title")]         public string? Title        { get; init; }
    [JsonPropertyName("Company")]       public string? Company      { get; init; }
    [JsonPropertyName("Industry")]      public string? Industry     { get; init; }
    [JsonPropertyName("Website")]       public string? Website      { get; init; }
    [JsonPropertyName("Lead_Status")]   public string? LeadStatus   { get; init; }
    [JsonPropertyName("Lead_Source")]   public string? LeadSource   { get; init; }
    [JsonPropertyName("Rating")]        public string? Rating       { get; init; }
    [JsonPropertyName("Description")]   public string? Description  { get; init; }
    [JsonPropertyName("Street")]        public string? Street       { get; init; }
    [JsonPropertyName("City")]          public string? City         { get; init; }
    [JsonPropertyName("State")]         public string? State        { get; init; }
    [JsonPropertyName("Zip_Code")]      public string? ZipCode      { get; init; }
    [JsonPropertyName("Country")]       public string? Country      { get; init; }
    [JsonPropertyName("Created_Time")]  public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")] public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]         public ZohoOwnerDto? Owner  { get; init; }
}

public sealed class ZohoContactDto
{
    public const string Fields =
        "id,First_Name,Last_Name,Full_Name,Email,Phone,Mobile,Title,Department,Description," +
        "Account_Name,Mailing_Street,Mailing_City,Mailing_State,Mailing_Zip,Mailing_Country," +
        "Email_Opt_Out,Do_Not_Call,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]              public string  Id            { get; init; } = "";
    [JsonPropertyName("First_Name")]      public string? FirstName     { get; init; }
    [JsonPropertyName("Last_Name")]       public string? LastName      { get; init; }
    [JsonPropertyName("Full_Name")]       public string? FullName      { get; init; }
    [JsonPropertyName("Email")]           public string? Email         { get; init; }
    [JsonPropertyName("Phone")]           public string? Phone         { get; init; }
    [JsonPropertyName("Mobile")]          public string? Mobile        { get; init; }
    [JsonPropertyName("Title")]           public string? Title         { get; init; }
    [JsonPropertyName("Department")]      public string? Department    { get; init; }
    [JsonPropertyName("Description")]     public string? Description   { get; init; }
    [JsonPropertyName("Account_Name")]    public ZohoAccountRefDto? AccountName { get; init; }
    [JsonPropertyName("Mailing_Street")]  public string? MailingStreet  { get; init; }
    [JsonPropertyName("Mailing_City")]    public string? MailingCity    { get; init; }
    [JsonPropertyName("Mailing_State")]   public string? MailingState   { get; init; }
    [JsonPropertyName("Mailing_Zip")]     public string? MailingZip     { get; init; }
    [JsonPropertyName("Mailing_Country")] public string? MailingCountry { get; init; }
    [JsonPropertyName("Email_Opt_Out")]   public bool?   EmailOptOut    { get; init; }
    [JsonPropertyName("Do_Not_Call")]     public bool?   DoNotCall      { get; init; }
    [JsonPropertyName("Created_Time")]    public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]   public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]           public ZohoOwnerDto? Owner   { get; init; }
}

public sealed class ZohoAccountDto
{
    public const string Fields =
        "id,Account_Name,Phone,Website,Industry,Description,Account_Type,Annual_Revenue,Employees," +
        "Billing_Street,Billing_City,Billing_State,Billing_Code,Billing_Country," +
        "Shipping_Street,Shipping_City,Shipping_State,Shipping_Code,Shipping_Country," +
        "Parent_Account,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]               public string  Id            { get; init; } = "";
    [JsonPropertyName("Account_Name")]     public string? AccountName   { get; init; }
    [JsonPropertyName("Phone")]            public string? Phone         { get; init; }
    [JsonPropertyName("Website")]          public string? Website       { get; init; }
    [JsonPropertyName("Industry")]         public string? Industry      { get; init; }
    [JsonPropertyName("Description")]      public string? Description   { get; init; }
    [JsonPropertyName("Account_Type")]     public string? AccountType   { get; init; }
    [JsonPropertyName("Annual_Revenue")]   public decimal? AnnualRevenue { get; init; }
    [JsonPropertyName("Employees")]        public int?    Employees     { get; init; }
    [JsonPropertyName("Billing_Street")]   public string? BillingStreet { get; init; }
    [JsonPropertyName("Billing_City")]     public string? BillingCity   { get; init; }
    [JsonPropertyName("Billing_State")]    public string? BillingState  { get; init; }
    [JsonPropertyName("Billing_Code")]     public string? BillingCode   { get; init; }
    [JsonPropertyName("Billing_Country")]  public string? BillingCountry { get; init; }
    [JsonPropertyName("Shipping_Street")]  public string? ShippingStreet { get; init; }
    [JsonPropertyName("Shipping_City")]    public string? ShippingCity   { get; init; }
    [JsonPropertyName("Shipping_State")]   public string? ShippingState  { get; init; }
    [JsonPropertyName("Shipping_Code")]    public string? ShippingCode   { get; init; }
    [JsonPropertyName("Shipping_Country")] public string? ShippingCountry { get; init; }
    [JsonPropertyName("Parent_Account")]   public ZohoAccountRefDto? ParentAccount { get; init; }
    [JsonPropertyName("Created_Time")]     public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]    public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]            public ZohoOwnerDto? Owner   { get; init; }
}

public sealed class ZohoDealDto
{
    public const string Fields =
        "id,Deal_Name,Account_Name,Contact_Name,Stage,Amount,Closing_Date,Probability," +
        "Lead_Source,Type,Description,Next_Step,Created_Time,Modified_Time,Last_Activity_Time,Owner";

    [JsonPropertyName("id")]            public string  Id           { get; init; } = "";
    [JsonPropertyName("Deal_Name")]     public string? DealName     { get; init; }
    [JsonPropertyName("Account_Name")]  public ZohoAccountRefDto? AccountName { get; init; }
    [JsonPropertyName("Contact_Name")]  public ZohoContactRefDto? ContactName { get; init; }
    [JsonPropertyName("Stage")]         public string? Stage        { get; init; }
    [JsonPropertyName("Amount")]        public decimal? Amount      { get; init; }
    [JsonPropertyName("Closing_Date")]  public DateTimeOffset? ClosingDate  { get; init; }
    [JsonPropertyName("Probability")]   public int?    Probability  { get; init; }
    [JsonPropertyName("Lead_Source")]   public string? LeadSource   { get; init; }
    [JsonPropertyName("Type")]          public string? Type         { get; init; }
    [JsonPropertyName("Description")]   public string? Description  { get; init; }
    [JsonPropertyName("Next_Step")]     public string? NextStep     { get; init; }
    [JsonPropertyName("Created_Time")]  public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")] public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Last_Activity_Time")] public DateTimeOffset? LastActivityTime { get; init; }
    [JsonPropertyName("Owner")]         public ZohoOwnerDto? Owner  { get; init; }
}

// ─── Lookup reference DTOs reused across modules ──────────────────────────

public sealed class ZohoLeadRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoDealRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoQuoteRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoOrderRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoProductRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoCampaignRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

// ─── Products ──────────────────────────────────────────────────────────────

public sealed class ZohoProductDto
{
    public const string Fields =
        "id,Product_Name,Product_Code,Product_Category,Product_Active,Description," +
        "Unit_Price,Unit_of_Measure,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]               public string  Id           { get; init; } = "";
    [JsonPropertyName("Product_Name")]     public string? ProductName  { get; init; }
    [JsonPropertyName("Product_Code")]     public string? ProductCode  { get; init; }
    [JsonPropertyName("Product_Category")] public string? Category     { get; init; }
    [JsonPropertyName("Product_Active")]   public bool?   IsActive     { get; init; }
    [JsonPropertyName("Description")]      public string? Description  { get; init; }
    [JsonPropertyName("Unit_Price")]       public decimal? UnitPrice   { get; init; }
    [JsonPropertyName("Unit_of_Measure")]  public string? Unit         { get; init; }
    [JsonPropertyName("Created_Time")]     public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]    public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]            public ZohoOwnerDto? Owner  { get; init; }
}

// ─── Quotes ────────────────────────────────────────────────────────────────

public sealed class ZohoQuoteLineDto
{
    [JsonPropertyName("id")]                public string? Id          { get; init; }
    [JsonPropertyName("product")]           public ZohoProductRefDto? Product { get; init; }
    [JsonPropertyName("quantity")]          public decimal? Quantity   { get; init; }
    [JsonPropertyName("list_price")]        public decimal? ListPrice  { get; init; }
    [JsonPropertyName("Discount")]          public decimal? Discount   { get; init; }
    [JsonPropertyName("total")]             public decimal? Total      { get; init; }
    [JsonPropertyName("net_total")]         public decimal? NetTotal   { get; init; }
    [JsonPropertyName("product_description")] public string? Description { get; init; }
}

public sealed class ZohoQuoteDto
{
    public const string Fields =
        "id,Subject,Quote_Stage,Account_Name,Contact_Name,Deal_Name,Valid_Till,Sub_Total,Discount,Tax,Grand_Total," +
        "Description,Quoted_Items,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]            public string  Id           { get; init; } = "";
    [JsonPropertyName("Subject")]       public string? Subject      { get; init; }
    [JsonPropertyName("Quote_Stage")]   public string? Stage        { get; init; }
    [JsonPropertyName("Account_Name")]  public ZohoAccountRefDto? AccountName { get; init; }
    [JsonPropertyName("Contact_Name")]  public ZohoContactRefDto? ContactName { get; init; }
    [JsonPropertyName("Deal_Name")]     public ZohoDealRefDto?    DealName    { get; init; }
    [JsonPropertyName("Valid_Till")]    public DateTimeOffset? ValidTill  { get; init; }
    [JsonPropertyName("Sub_Total")]     public decimal? Subtotal     { get; init; }
    [JsonPropertyName("Discount")]      public decimal? Discount     { get; init; }
    [JsonPropertyName("Tax")]           public decimal? Tax          { get; init; }
    [JsonPropertyName("Grand_Total")]   public decimal? GrandTotal   { get; init; }
    [JsonPropertyName("Description")]   public string? Description   { get; init; }
    [JsonPropertyName("Quoted_Items")]  public List<ZohoQuoteLineDto>? Lines { get; init; }
    [JsonPropertyName("Created_Time")]  public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")] public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]         public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Activities (Tasks / Calls / Events all live in /Activities) ───────────

public sealed class ZohoActivityRelatedDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("module")] public string? Module { get; init; }
}

public sealed class ZohoActivityDto
{
    public const string Fields =
        "id,Subject,Status,Priority,Description,Due_Date,Activity_Type,Call_Type,Call_Duration," +
        "Call_Start_Time," +
        "Start_DateTime,End_DateTime,Event_Title,Venue,What_Id,Who_Id,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]              public string  Id            { get; init; } = "";
    [JsonPropertyName("Subject")]         public string? Subject       { get; init; }
    [JsonPropertyName("Status")]          public string? Status        { get; init; }
    [JsonPropertyName("Priority")]        public string? Priority      { get; init; }
    [JsonPropertyName("Description")]     public string? Description   { get; init; }
    [JsonPropertyName("Due_Date")]        public DateTimeOffset? DueDate { get; init; }
    [JsonPropertyName("Activity_Type")]   public string? ActivityType  { get; init; }
    [JsonPropertyName("Call_Type")]       public string? CallType      { get; init; }
    [JsonPropertyName("Call_Duration")]   public string? CallDuration  { get; init; }
    [JsonPropertyName("Call_Start_Time")] public DateTimeOffset? CallStartTime { get; init; }
    [JsonPropertyName("Start_DateTime")]  public DateTimeOffset? StartDateTime { get; init; }
    [JsonPropertyName("End_DateTime")]    public DateTimeOffset? EndDateTime   { get; init; }
    [JsonPropertyName("Event_Title")]     public string? EventTitle    { get; init; }
    [JsonPropertyName("Venue")]           public string? Venue         { get; init; }
    [JsonPropertyName("What_Id")]         public ZohoActivityRelatedDto? WhatId { get; init; }
    [JsonPropertyName("Who_Id")]          public ZohoActivityRelatedDto? WhoId  { get; init; }
    [JsonPropertyName("Created_Time")]    public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]   public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]           public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Campaigns ─────────────────────────────────────────────────────────────

public sealed class ZohoCampaignDto
{
    public const string Fields =
        "id,Campaign_Name,Type,Status,Start_Date,End_Date,Description,Budgeted_Cost,Actual_Cost," +
        "Expected_Revenue,Num_sent,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]              public string  Id            { get; init; } = "";
    [JsonPropertyName("Campaign_Name")]   public string? Name          { get; init; }
    [JsonPropertyName("Type")]            public string? Type          { get; init; }
    [JsonPropertyName("Status")]          public string? Status        { get; init; }
    [JsonPropertyName("Start_Date")]      public DateTimeOffset? StartDate { get; init; }
    [JsonPropertyName("End_Date")]        public DateTimeOffset? EndDate   { get; init; }
    [JsonPropertyName("Description")]     public string? Description   { get; init; }
    [JsonPropertyName("Budgeted_Cost")]   public decimal? BudgetedCost { get; init; }
    [JsonPropertyName("Actual_Cost")]     public decimal? ActualCost   { get; init; }
    [JsonPropertyName("Expected_Revenue")] public decimal? ExpectedRevenue { get; init; }
    [JsonPropertyName("Num_sent")]        public int?    NumSent       { get; init; }
    [JsonPropertyName("Created_Time")]    public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]   public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]           public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Cases (→ our Tickets) ────────────────────────────────────────────────

public sealed class ZohoCaseDto
{
    public const string Fields =
        "id,Case_Number,Subject,Description,Account_Name,Contact_Name,Status,Priority,Type," +
        "Case_Origin,Reported_By,Closed_Time,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]              public string  Id            { get; init; } = "";
    [JsonPropertyName("Case_Number")]     public string? CaseNumber    { get; init; }
    [JsonPropertyName("Subject")]         public string? Subject       { get; init; }
    [JsonPropertyName("Description")]     public string? Description   { get; init; }
    [JsonPropertyName("Account_Name")]    public ZohoAccountRefDto? AccountName { get; init; }
    [JsonPropertyName("Contact_Name")]    public ZohoContactRefDto? ContactName { get; init; }
    [JsonPropertyName("Status")]          public string? Status        { get; init; }
    [JsonPropertyName("Priority")]        public string? Priority      { get; init; }
    [JsonPropertyName("Type")]            public string? Type          { get; init; }
    [JsonPropertyName("Case_Origin")]     public string? CaseOrigin    { get; init; }
    [JsonPropertyName("Reported_By")]     public string? ReportedBy    { get; init; }
    [JsonPropertyName("Closed_Time")]     public DateTimeOffset? ClosedTime { get; init; }
    [JsonPropertyName("Created_Time")]    public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]   public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]           public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Invoices ──────────────────────────────────────────────────────────────

public sealed class ZohoInvoiceLineDto
{
    [JsonPropertyName("id")]                public string? Id          { get; init; }
    [JsonPropertyName("product")]           public ZohoProductRefDto? Product { get; init; }
    [JsonPropertyName("quantity")]          public decimal? Quantity   { get; init; }
    [JsonPropertyName("list_price")]        public decimal? ListPrice  { get; init; }
    [JsonPropertyName("net_total")]         public decimal? NetTotal   { get; init; }
    [JsonPropertyName("product_description")] public string? Description { get; init; }
}

public sealed class ZohoInvoiceDto
{
    public const string Fields =
        "id,Invoice_Number,Subject,Status,Account_Name,Sales_Order,Invoice_Date,Due_Date," +
        "Sub_Total,Tax,Grand_Total,Balance,Description,Invoiced_Items," +
        "Billing_Street,Billing_City,Billing_State,Billing_Code,Billing_Country," +
        "Shipping_Street,Shipping_City,Shipping_State,Shipping_Code,Shipping_Country," +
        "Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]              public string  Id            { get; init; } = "";
    [JsonPropertyName("Invoice_Number")]  public string? InvoiceNumber { get; init; }
    [JsonPropertyName("Subject")]         public string? Subject       { get; init; }
    [JsonPropertyName("Status")]          public string? Status        { get; init; }
    [JsonPropertyName("Account_Name")]    public ZohoAccountRefDto? AccountName { get; init; }
    [JsonPropertyName("Sales_Order")]     public ZohoOrderRefDto?   SalesOrder { get; init; }
    [JsonPropertyName("Invoice_Date")]    public DateTimeOffset? InvoiceDate { get; init; }
    [JsonPropertyName("Due_Date")]        public DateTimeOffset? DueDate { get; init; }
    [JsonPropertyName("Sub_Total")]       public decimal? Subtotal     { get; init; }
    [JsonPropertyName("Tax")]             public decimal? Tax          { get; init; }
    [JsonPropertyName("Grand_Total")]     public decimal? GrandTotal   { get; init; }
    [JsonPropertyName("Balance")]         public decimal? Balance      { get; init; }
    [JsonPropertyName("Description")]     public string? Description   { get; init; }
    [JsonPropertyName("Invoiced_Items")]  public List<ZohoInvoiceLineDto>? Lines { get; init; }
    [JsonPropertyName("Billing_Street")]  public string? BillingStreet  { get; init; }
    [JsonPropertyName("Billing_City")]    public string? BillingCity    { get; init; }
    [JsonPropertyName("Billing_State")]   public string? BillingState   { get; init; }
    [JsonPropertyName("Billing_Code")]    public string? BillingCode    { get; init; }
    [JsonPropertyName("Billing_Country")] public string? BillingCountry { get; init; }
    [JsonPropertyName("Shipping_Street")] public string? ShippingStreet { get; init; }
    [JsonPropertyName("Shipping_City")]   public string? ShippingCity   { get; init; }
    [JsonPropertyName("Shipping_State")]  public string? ShippingState  { get; init; }
    [JsonPropertyName("Shipping_Code")]   public string? ShippingCode   { get; init; }
    [JsonPropertyName("Shipping_Country")] public string? ShippingCountry { get; init; }
    [JsonPropertyName("Created_Time")]    public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]   public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]           public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Sales_Orders (→ our Orders) ───────────────────────────────────────────

public sealed class ZohoSalesOrderLineDto
{
    [JsonPropertyName("id")]                public string? Id          { get; init; }
    [JsonPropertyName("product")]           public ZohoProductRefDto? Product { get; init; }
    [JsonPropertyName("quantity")]          public decimal? Quantity   { get; init; }
    [JsonPropertyName("list_price")]        public decimal? ListPrice  { get; init; }
    [JsonPropertyName("Discount")]          public decimal? Discount   { get; init; }
    [JsonPropertyName("net_total")]         public decimal? NetTotal   { get; init; }
    [JsonPropertyName("product_description")] public string? Description { get; init; }
}

public sealed class ZohoSalesOrderDto
{
    public const string Fields =
        "id,SO_Number,Subject,Status,Account_Name,Deal_Name,Quote_Name,Sub_Total,Discount,Tax,Grand_Total," +
        "Description,Ordered_Items," +
        "Billing_Street,Billing_City,Billing_State,Billing_Code,Billing_Country," +
        "Shipping_Street,Shipping_City,Shipping_State,Shipping_Code,Shipping_Country," +
        "Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]              public string  Id            { get; init; } = "";
    [JsonPropertyName("SO_Number")]       public string? OrderNumber   { get; init; }
    [JsonPropertyName("Subject")]         public string? Subject       { get; init; }
    [JsonPropertyName("Status")]          public string? Status        { get; init; }
    [JsonPropertyName("Account_Name")]    public ZohoAccountRefDto? AccountName { get; init; }
    [JsonPropertyName("Deal_Name")]       public ZohoDealRefDto?    DealName    { get; init; }
    [JsonPropertyName("Quote_Name")]      public ZohoQuoteRefDto?   QuoteName   { get; init; }
    [JsonPropertyName("Sub_Total")]       public decimal? Subtotal     { get; init; }
    [JsonPropertyName("Discount")]        public decimal? Discount     { get; init; }
    [JsonPropertyName("Tax")]             public decimal? Tax          { get; init; }
    [JsonPropertyName("Grand_Total")]     public decimal? GrandTotal   { get; init; }
    [JsonPropertyName("Description")]     public string? Description   { get; init; }
    [JsonPropertyName("Ordered_Items")]   public List<ZohoSalesOrderLineDto>? Lines { get; init; }
    [JsonPropertyName("Billing_Street")]  public string? BillingStreet  { get; init; }
    [JsonPropertyName("Billing_City")]    public string? BillingCity    { get; init; }
    [JsonPropertyName("Billing_State")]   public string? BillingState   { get; init; }
    [JsonPropertyName("Billing_Code")]    public string? BillingCode    { get; init; }
    [JsonPropertyName("Billing_Country")] public string? BillingCountry { get; init; }
    [JsonPropertyName("Shipping_Street")] public string? ShippingStreet { get; init; }
    [JsonPropertyName("Shipping_City")]   public string? ShippingCity   { get; init; }
    [JsonPropertyName("Shipping_State")]  public string? ShippingState  { get; init; }
    [JsonPropertyName("Shipping_Code")]   public string? ShippingCode   { get; init; }
    [JsonPropertyName("Shipping_Country")] public string? ShippingCountry { get; init; }
    [JsonPropertyName("Created_Time")]    public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]   public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]           public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Notes ─────────────────────────────────────────────────────────────────

public sealed class ZohoNoteParentDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("module")] public ZohoNoteModuleDto? Module { get; init; }
}

public sealed class ZohoNoteModuleDto
{
    [JsonPropertyName("api_name")] public string? ApiName { get; init; }
    [JsonPropertyName("id")]       public string? Id      { get; init; }
}

public sealed class ZohoNoteDto
{
    public const string Fields =
        "id,Note_Title,Note_Content,Parent_Id,$se_module,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]            public string  Id           { get; init; } = "";
    [JsonPropertyName("Note_Title")]    public string? Title        { get; init; }
    [JsonPropertyName("Note_Content")]  public string? Content      { get; init; }
    [JsonPropertyName("Parent_Id")]     public ZohoNoteParentDto? ParentId { get; init; }
    [JsonPropertyName("$se_module")]    public string? SeModule     { get; init; }
    [JsonPropertyName("Created_Time")]  public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")] public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]         public ZohoOwnerDto? Owner  { get; init; }
}

// ─── Vendors ──────────────────────────────────────────────────────────────

public sealed class ZohoVendorRefDto
{
    [JsonPropertyName("id")]   public string? Id   { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
}

public sealed class ZohoVendorDto
{
    public const string Fields =
        "id,Vendor_Name,Email,Phone,Website,Description,Category,GL_Account," +
        "Street,City,State,Zip_Code,Country,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]            public string  Id           { get; init; } = "";
    [JsonPropertyName("Vendor_Name")]   public string? Name         { get; init; }
    [JsonPropertyName("Email")]         public string? Email        { get; init; }
    [JsonPropertyName("Phone")]         public string? Phone        { get; init; }
    [JsonPropertyName("Website")]       public string? Website      { get; init; }
    [JsonPropertyName("Description")]   public string? Description  { get; init; }
    [JsonPropertyName("Category")]      public string? Category     { get; init; }
    [JsonPropertyName("GL_Account")]    public string? GlAccount    { get; init; }
    [JsonPropertyName("Street")]        public string? Street       { get; init; }
    [JsonPropertyName("City")]          public string? City         { get; init; }
    [JsonPropertyName("State")]         public string? State        { get; init; }
    [JsonPropertyName("Zip_Code")]      public string? ZipCode      { get; init; }
    [JsonPropertyName("Country")]       public string? Country      { get; init; }
    [JsonPropertyName("Created_Time")]  public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")] public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]         public ZohoOwnerDto? Owner  { get; init; }
}

// ─── Purchase Orders ──────────────────────────────────────────────────────

public sealed class ZohoPurchaseOrderLineDto
{
    [JsonPropertyName("id")]                public string? Id          { get; init; }
    [JsonPropertyName("product")]           public ZohoProductRefDto? Product { get; init; }
    [JsonPropertyName("quantity")]          public decimal? Quantity   { get; init; }
    [JsonPropertyName("list_price")]        public decimal? ListPrice  { get; init; }
    [JsonPropertyName("Discount")]          public decimal? Discount   { get; init; }
    [JsonPropertyName("net_total")]         public decimal? NetTotal   { get; init; }
    [JsonPropertyName("product_description")] public string? Description { get; init; }
}

public sealed class ZohoPurchaseOrderDto
{
    public const string Fields =
        "id,PO_Number,Subject,Status,Vendor_Name,Requisition_No,PO_Date,Due_Date,Carrier," +
        "Sub_Total,Discount,Tax,Adjustment,Grand_Total,Description,Terms_and_Conditions,Purchase_Items," +
        "Billing_Street,Billing_City,Billing_State,Billing_Code,Billing_Country," +
        "Shipping_Street,Shipping_City,Shipping_State,Shipping_Code,Shipping_Country," +
        "Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]               public string  Id            { get; init; } = "";
    [JsonPropertyName("PO_Number")]        public string? PoNumber      { get; init; }
    [JsonPropertyName("Subject")]          public string? Subject       { get; init; }
    [JsonPropertyName("Status")]           public string? Status        { get; init; }
    [JsonPropertyName("Vendor_Name")]      public ZohoVendorRefDto? VendorName { get; init; }
    [JsonPropertyName("Requisition_No")]   public string? RequisitionNo { get; init; }
    [JsonPropertyName("PO_Date")]          public DateTimeOffset? PoDate { get; init; }
    [JsonPropertyName("Due_Date")]         public DateTimeOffset? DueDate { get; init; }
    [JsonPropertyName("Carrier")]          public string? Carrier       { get; init; }
    [JsonPropertyName("Sub_Total")]        public decimal? Subtotal     { get; init; }
    [JsonPropertyName("Discount")]         public decimal? Discount     { get; init; }
    [JsonPropertyName("Tax")]              public decimal? Tax          { get; init; }
    [JsonPropertyName("Adjustment")]       public decimal? Adjustment   { get; init; }
    [JsonPropertyName("Grand_Total")]      public decimal? GrandTotal   { get; init; }
    [JsonPropertyName("Description")]      public string? Description   { get; init; }
    [JsonPropertyName("Terms_and_Conditions")] public string? Terms     { get; init; }
    [JsonPropertyName("Purchase_Items")]   public List<ZohoPurchaseOrderLineDto>? Lines { get; init; }
    [JsonPropertyName("Billing_Street")]   public string? BillingStreet  { get; init; }
    [JsonPropertyName("Billing_City")]     public string? BillingCity    { get; init; }
    [JsonPropertyName("Billing_State")]    public string? BillingState   { get; init; }
    [JsonPropertyName("Billing_Code")]     public string? BillingCode    { get; init; }
    [JsonPropertyName("Billing_Country")]  public string? BillingCountry { get; init; }
    [JsonPropertyName("Shipping_Street")]  public string? ShippingStreet { get; init; }
    [JsonPropertyName("Shipping_City")]    public string? ShippingCity   { get; init; }
    [JsonPropertyName("Shipping_State")]   public string? ShippingState  { get; init; }
    [JsonPropertyName("Shipping_Code")]    public string? ShippingCode   { get; init; }
    [JsonPropertyName("Shipping_Country")] public string? ShippingCountry { get; init; }
    [JsonPropertyName("Created_Time")]     public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]    public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]            public ZohoOwnerDto? Owner   { get; init; }
}

// ─── Solutions ────────────────────────────────────────────────────────────

public sealed class ZohoSolutionDto
{
    public const string Fields =
        "id,Solution_Number,Solution_Title,Question,Answer,Solution_Category,Status,Product_Name," +
        "Published_Solution,Comments,Created_Time,Modified_Time,Owner";

    [JsonPropertyName("id")]                  public string  Id             { get; init; } = "";
    [JsonPropertyName("Solution_Number")]     public string? SolutionNumber { get; init; }
    [JsonPropertyName("Solution_Title")]      public string? Title          { get; init; }
    [JsonPropertyName("Question")]            public string? Question       { get; init; }
    [JsonPropertyName("Answer")]              public string? Answer         { get; init; }
    [JsonPropertyName("Solution_Category")]   public string? Category       { get; init; }
    [JsonPropertyName("Status")]              public string? Status         { get; init; }
    [JsonPropertyName("Product_Name")]        public ZohoProductRefDto? ProductName { get; init; }
    [JsonPropertyName("Published_Solution")]  public bool?   Published      { get; init; }
    [JsonPropertyName("Comments")]            public string? Comments       { get; init; }
    [JsonPropertyName("Created_Time")]        public DateTimeOffset? CreatedTime  { get; init; }
    [JsonPropertyName("Modified_Time")]       public DateTimeOffset? ModifiedTime { get; init; }
    [JsonPropertyName("Owner")]               public ZohoOwnerDto? Owner    { get; init; }
}

// ─── Field metadata (used to discover custom fields per module) ──────────

public sealed class ZohoFieldsResponse
{
    [JsonPropertyName("fields")] public IReadOnlyList<ZohoFieldMetadataDto> Fields { get; init; } = Array.Empty<ZohoFieldMetadataDto>();
}

public sealed class ZohoFieldMetadataDto
{
    [JsonPropertyName("api_name")]      public string? ApiName     { get; init; }
    [JsonPropertyName("field_label")]   public string? FieldLabel  { get; init; }
    [JsonPropertyName("data_type")]     public string? DataType    { get; init; }
    [JsonPropertyName("custom_field")]  public bool    CustomField { get; init; }
    [JsonPropertyName("read_only")]     public bool    ReadOnly    { get; init; }
}

public sealed record ZohoHealthDto(bool Configured, string Region, bool TokenAcquired, string? Error);

public sealed record ZohoConnectionTestDto(
    bool Ok,
    bool ConnectionExists,
    bool Configured,
    bool Connected,
    bool TokenAcquired,
    bool CrmApiReachable,
    string Region,
    DateTime CheckedAtUtc,
    string? Message,
    string? Error,
    int? StatusCode,
    int? RetryAfterSeconds);
