using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiLogSessionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_crm_ai_interaction_log_Success",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.AddColumn<string>(
                name: "AfterImageBase64",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeImageBase64",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalStep",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "SourceDpi",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceFileName",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StepNumber",
                schema: "crm",
                table: "crm_ai_interaction_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_interaction_log_IsFinalStep",
                schema: "crm",
                table: "crm_ai_interaction_log",
                column: "IsFinalStep");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_interaction_log_SessionId",
                schema: "crm",
                table: "crm_ai_interaction_log",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_crm_ai_interaction_log_IsFinalStep",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropIndex(
                name: "IX_crm_ai_interaction_log_SessionId",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "AfterImageBase64",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "BeforeImageBase64",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "IsFinalStep",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "SourceDpi",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "SourceFileName",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.DropColumn(
                name: "StepNumber",
                schema: "crm",
                table: "crm_ai_interaction_log");

            migrationBuilder.CreateIndex(
                name: "IX_crm_ai_interaction_log_Success",
                schema: "crm",
                table: "crm_ai_interaction_log",
                column: "Success");
        }
    }
}
