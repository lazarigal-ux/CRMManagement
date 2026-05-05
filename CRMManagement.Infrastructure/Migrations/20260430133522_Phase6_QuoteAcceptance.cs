using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_QuoteAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                schema: "crm",
                table: "crm_quotes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByEmail",
                schema: "crm",
                table: "crm_quotes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedByName",
                schema: "crm",
                table: "crm_quotes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptedFromIp",
                schema: "crm",
                table: "crm_quotes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureSvg",
                schema: "crm",
                table: "crm_quotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SignatureToken",
                schema: "crm",
                table: "crm_quotes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_quotes_SignatureToken",
                schema: "crm",
                table: "crm_quotes",
                column: "SignatureToken",
                unique: true,
                filter: "\"SignatureToken\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_crm_quotes_SignatureToken",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "AcceptedByEmail",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "AcceptedByName",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "AcceptedFromIp",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "SignatureSvg",
                schema: "crm",
                table: "crm_quotes");

            migrationBuilder.DropColumn(
                name: "SignatureToken",
                schema: "crm",
                table: "crm_quotes");
        }
    }
}
