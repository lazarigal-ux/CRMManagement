using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_IntegrationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crm_integration_outbox",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Target = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RelatedType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RelatedId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_integration_outbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_integration_outbox_RelatedType_RelatedId",
                schema: "crm",
                table: "crm_integration_outbox",
                columns: new[] { "RelatedType", "RelatedId" });

            migrationBuilder.CreateIndex(
                name: "IX_crm_integration_outbox_Status",
                schema: "crm",
                table: "crm_integration_outbox",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_crm_integration_outbox_Target",
                schema: "crm",
                table: "crm_integration_outbox",
                column: "Target");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_integration_outbox",
                schema: "crm");
        }
    }
}
