using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiExamplesAndLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crm_ai_examples",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: false),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    BeforeImageBase64 = table.Column<string>(type: "text", nullable: true),
                    AfterImageBase64 = table.Column<string>(type: "text", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    ResultText = table.Column<string>(type: "text", nullable: true),
                    Rating = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_ai_examples", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crm_ai_interaction_log",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    Instruction = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultText = table.Column<string>(type: "text", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    TotalMs = table.Column<int>(type: "integer", nullable: true),
                    NetMs = table.Column<int>(type: "integer", nullable: true),
                    ExamplesUsed = table.Column<int>(type: "integer", nullable: false),
                    Feedback = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    AiExampleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_ai_interaction_log", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_examples_Category",
                schema: "crm",
                table: "crm_ai_examples",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_examples_Rating",
                schema: "crm",
                table: "crm_ai_examples",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_examples_Tags",
                schema: "crm",
                table: "crm_ai_examples",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_interaction_log_CreatedAt",
                schema: "crm",
                table: "crm_ai_interaction_log",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_interaction_log_Provider",
                schema: "crm",
                table: "crm_ai_interaction_log",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_interaction_log_Success",
                schema: "crm",
                table: "crm_ai_interaction_log",
                column: "Success");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_ai_examples",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_ai_interaction_log",
                schema: "crm");
        }
    }
}
