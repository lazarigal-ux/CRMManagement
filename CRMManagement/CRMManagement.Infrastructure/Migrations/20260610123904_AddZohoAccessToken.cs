using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZohoAccessToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccessTokenExpiresAt",
                schema: "crm",
                table: "crm_zoho_connection",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessTokenProtected",
                schema: "crm",
                table: "crm_zoho_connection",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessTokenExpiresAt",
                schema: "crm",
                table: "crm_zoho_connection");

            migrationBuilder.DropColumn(
                name: "AccessTokenProtected",
                schema: "crm",
                table: "crm_zoho_connection");
        }
    }
}
