using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_ZohoFullSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivitiesErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActivitiesInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActivitiesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActivitiesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignsErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignsInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CampaignsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoicesErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoicesInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoicesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InvoicesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotesErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotesInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrdersErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrdersInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrdersSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OrdersUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductsErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductsInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProductsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuotesErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuotesInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuotesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuotesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TicketsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_tickets",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_quotes",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_quote_lines",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_products",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_orders",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_order_lines",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "crm",
                table: "crm_opportunities",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                schema: "crm",
                table: "crm_notes",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_notes",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualRevenue",
                schema: "crm",
                table: "crm_leads",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mobile",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoOfEmployees",
                schema: "crm",
                table: "crm_leads",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_leads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_invoices",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_invoice_lines",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_contacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumSent",
                schema: "crm",
                table: "crm_campaigns",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_campaigns",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_campaigns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_activities",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountType",
                schema: "crm",
                table: "crm_accounts",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_tickets_ZohoId",
                schema: "crm",
                table: "crm_tickets",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_quotes_ZohoId",
                schema: "crm",
                table: "crm_quotes",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_quote_lines_ZohoId",
                schema: "crm",
                table: "crm_quote_lines",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_products_ZohoId",
                schema: "crm",
                table: "crm_products",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_orders_ZohoId",
                schema: "crm",
                table: "crm_orders",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_order_lines_ZohoId",
                schema: "crm",
                table: "crm_order_lines",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_notes_ZohoId",
                schema: "crm",
                table: "crm_notes",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_invoices_ZohoId",
                schema: "crm",
                table: "crm_invoices",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_invoice_lines_ZohoId",
                schema: "crm",
                table: "crm_invoice_lines",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_campaigns_ZohoId",
                schema: "crm",
                table: "crm_campaigns",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_activities_ZohoId",
                schema: "crm",
                table: "crm_activities",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_crm_tickets_ZohoId",
                schema: "crm",
                table: "crm_tickets");

            migrationBuilder.DropIndex(
                name: "IX_crm_quotes_ZohoId",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropIndex(
                name: "IX_crm_quote_lines_ZohoId",
                schema: "crm",
                table: "crm_quote_lines");

            migrationBuilder.DropIndex(
                name: "IX_crm_products_ZohoId",
                schema: "crm",
                table: "crm_products");

            migrationBuilder.DropIndex(
                name: "IX_crm_orders_ZohoId",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropIndex(
                name: "IX_crm_order_lines_ZohoId",
                schema: "crm",
                table: "crm_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_crm_notes_ZohoId",
                schema: "crm",
                table: "crm_notes");

            migrationBuilder.DropIndex(
                name: "IX_crm_invoices_ZohoId",
                schema: "crm",
                table: "crm_invoices");

            migrationBuilder.DropIndex(
                name: "IX_crm_invoice_lines_ZohoId",
                schema: "crm",
                table: "crm_invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_crm_campaigns_ZohoId",
                schema: "crm",
                table: "crm_campaigns");

            migrationBuilder.DropIndex(
                name: "IX_crm_activities_ZohoId",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "ActivitiesErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ActivitiesInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ActivitiesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ActivitiesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "CampaignsErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "CampaignsInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "CampaignsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "CampaignsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "InvoicesErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "InvoicesInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "InvoicesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "InvoicesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "NotesErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "NotesInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "NotesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "NotesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "OrdersErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "OrdersInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "OrdersSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "OrdersUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ProductsErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ProductsInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ProductsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ProductsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "QuotesErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "QuotesInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "QuotesSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "QuotesUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "TicketsErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "TicketsInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "TicketsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "TicketsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_tickets");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_tickets");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_tickets");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_quote_lines");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_products");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_products");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_products");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_order_lines");

            migrationBuilder.DropColumn(
                name: "Type",
                schema: "crm",
                table: "crm_opportunities");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_opportunities");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_opportunities");

            migrationBuilder.DropColumn(
                name: "Title",
                schema: "crm",
                table: "crm_notes");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_notes");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_notes");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_notes");

            migrationBuilder.DropColumn(
                name: "AnnualRevenue",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "Mobile",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "NoOfEmployees",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "State",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "Street",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_invoices");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_invoices");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_invoices");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_invoice_lines");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_contacts");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_contacts");

            migrationBuilder.DropColumn(
                name: "NumSent",
                schema: "crm",
                table: "crm_campaigns");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_campaigns");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_campaigns");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_campaigns");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "AccountType",
                schema: "crm",
                table: "crm_accounts");

            migrationBuilder.DropColumn(
                name: "ZohoCreatedTime",
                schema: "crm",
                table: "crm_accounts");

            migrationBuilder.DropColumn(
                name: "ZohoModifiedTime",
                schema: "crm",
                table: "crm_accounts");
        }
    }
}
