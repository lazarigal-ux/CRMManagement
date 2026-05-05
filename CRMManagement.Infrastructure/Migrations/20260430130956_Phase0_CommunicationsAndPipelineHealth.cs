using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase0_CommunicationsAndPipelineHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlaHours",
                schema: "crm",
                table: "crm_pipeline_stages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StageEnteredAt",
                schema: "crm",
                table: "crm_opportunities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "crm_communications",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FromAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ToAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_communications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_communications_AccountId",
                schema: "crm",
                table: "crm_communications",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_communications_ContactId",
                schema: "crm",
                table: "crm_communications",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_communications_LeadId",
                schema: "crm",
                table: "crm_communications",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_communications_OccurredAt",
                schema: "crm",
                table: "crm_communications",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_communications_OpportunityId",
                schema: "crm",
                table: "crm_communications",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_communications_Provider_ExternalId",
                schema: "crm",
                table: "crm_communications",
                columns: new[] { "Provider", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_communications",
                schema: "crm");

            migrationBuilder.DropColumn(
                name: "SlaHours",
                schema: "crm",
                table: "crm_pipeline_stages");

            migrationBuilder.DropColumn(
                name: "StageEnteredAt",
                schema: "crm",
                table: "crm_opportunities");
        }
    }
}
