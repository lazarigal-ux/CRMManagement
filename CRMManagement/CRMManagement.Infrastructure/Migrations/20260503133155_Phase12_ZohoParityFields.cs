using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase12_ZohoParityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportedBy",
                schema: "crm",
                table: "crm_tickets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequisitionNo",
                schema: "crm",
                table: "crm_purchase_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                schema: "crm",
                table: "crm_orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                schema: "crm",
                table: "crm_invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportedBy",
                schema: "crm",
                table: "crm_tickets");

            migrationBuilder.DropColumn(
                name: "RequisitionNo",
                schema: "crm",
                table: "crm_purchase_orders");

            migrationBuilder.DropColumn(
                name: "Subject",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropColumn(
                name: "Subject",
                schema: "crm",
                table: "crm_invoices");
        }
    }
}
