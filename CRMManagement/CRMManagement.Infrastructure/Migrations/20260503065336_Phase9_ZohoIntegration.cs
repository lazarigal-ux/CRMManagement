using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_ZohoIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_opportunities",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_leads",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_contacts",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZohoId",
                schema: "crm",
                table: "crm_accounts",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "crm_zoho_connection",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: true),
                    Region = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ClientSecretProtected = table.Column<string>(type: "text", nullable: false),
                    RefreshTokenProtected = table.Column<string>(type: "text", nullable: true),
                    AccountOwnerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountOwnerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisconnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastImportAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_zoho_connection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crm_zoho_import_job",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Modules = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LeadsInserted = table.Column<int>(type: "integer", nullable: false),
                    LeadsUpdated = table.Column<int>(type: "integer", nullable: false),
                    LeadsSkipped = table.Column<int>(type: "integer", nullable: false),
                    LeadsErrored = table.Column<int>(type: "integer", nullable: false),
                    ContactsInserted = table.Column<int>(type: "integer", nullable: false),
                    ContactsUpdated = table.Column<int>(type: "integer", nullable: false),
                    ContactsSkipped = table.Column<int>(type: "integer", nullable: false),
                    ContactsErrored = table.Column<int>(type: "integer", nullable: false),
                    AccountsInserted = table.Column<int>(type: "integer", nullable: false),
                    AccountsUpdated = table.Column<int>(type: "integer", nullable: false),
                    AccountsSkipped = table.Column<int>(type: "integer", nullable: false),
                    AccountsErrored = table.Column<int>(type: "integer", nullable: false),
                    DealsInserted = table.Column<int>(type: "integer", nullable: false),
                    DealsUpdated = table.Column<int>(type: "integer", nullable: false),
                    DealsSkipped = table.Column<int>(type: "integer", nullable: false),
                    DealsErrored = table.Column<int>(type: "integer", nullable: false),
                    ErrorsJson = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_zoho_import_job", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_opportunities_ZohoId",
                schema: "crm",
                table: "crm_opportunities",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_leads_ZohoId",
                schema: "crm",
                table: "crm_leads",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_contacts_ZohoId",
                schema: "crm",
                table: "crm_contacts",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_accounts_ZohoId",
                schema: "crm",
                table: "crm_accounts",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_zoho_connection_CompanyId",
                schema: "crm",
                table: "crm_zoho_connection",
                column: "CompanyId",
                unique: true,
                filter: "\"CompanyId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_zoho_import_job_StartedAt",
                schema: "crm",
                table: "crm_zoho_import_job",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_zoho_import_job_Status",
                schema: "crm",
                table: "crm_zoho_import_job",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_zoho_connection",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_zoho_import_job",
                schema: "crm");

            migrationBuilder.DropIndex(
                name: "IX_crm_opportunities_ZohoId",
                schema: "crm",
                table: "crm_opportunities");

            migrationBuilder.DropIndex(
                name: "IX_crm_leads_ZohoId",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropIndex(
                name: "IX_crm_contacts_ZohoId",
                schema: "crm",
                table: "crm_contacts");

            migrationBuilder.DropIndex(
                name: "IX_crm_accounts_ZohoId",
                schema: "crm",
                table: "crm_accounts");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_opportunities");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_leads");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_contacts");

            migrationBuilder.DropColumn(
                name: "ZohoId",
                schema: "crm",
                table: "crm_accounts");
        }
    }
}
