using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_VendorsPurchaseOrdersSolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Modules",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrdersErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrdersInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrdersSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrdersUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SolutionsErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SolutionsInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SolutionsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SolutionsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VendorsErrored",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VendorsInserted",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VendorsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VendorsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                schema: "crm",
                table: "crm_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                schema: "crm",
                table: "crm_orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                schema: "crm",
                table: "crm_invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                schema: "crm",
                table: "crm_invoices",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                schema: "crm",
                table: "crm_activities",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallDurationSeconds",
                schema: "crm",
                table: "crm_activities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallType",
                schema: "crm",
                table: "crm_activities",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventTitle",
                schema: "crm",
                table: "crm_activities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "crm_solutions",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Question = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Answer = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Published = table.Column<bool>(type: "boolean", nullable: false),
                    Comments = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ZohoId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ZohoCreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ZohoModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_solutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crm_vendors",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    State = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    GlAccount = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ZohoId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ZohoCreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ZohoModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "crm_purchase_orders",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequisitionedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PoDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CarrierName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TermsAndConditions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShippingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ZohoId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    ZohoCreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ZohoModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_purchase_orders_crm_vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "crm",
                        principalTable: "crm_vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crm_purchase_order_lines",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ZohoId = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_purchase_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_purchase_order_lines_crm_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "crm",
                        principalTable: "crm_purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_purchase_order_lines_PurchaseOrderId",
                schema: "crm",
                table: "crm_purchase_order_lines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_purchase_order_lines_ZohoId",
                schema: "crm",
                table: "crm_purchase_order_lines",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_purchase_orders_OwnerUserId",
                schema: "crm",
                table: "crm_purchase_orders",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_purchase_orders_PoNumber",
                schema: "crm",
                table: "crm_purchase_orders",
                column: "PoNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_purchase_orders_VendorId",
                schema: "crm",
                table: "crm_purchase_orders",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_purchase_orders_ZohoId",
                schema: "crm",
                table: "crm_purchase_orders",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_solutions_Title",
                schema: "crm",
                table: "crm_solutions",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_crm_solutions_ZohoId",
                schema: "crm",
                table: "crm_solutions",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_crm_vendors_Name",
                schema: "crm",
                table: "crm_vendors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_crm_vendors_OwnerUserId",
                schema: "crm",
                table: "crm_vendors",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_vendors_ZohoId",
                schema: "crm",
                table: "crm_vendors",
                column: "ZohoId",
                unique: true,
                filter: "\"ZohoId\" is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_purchase_order_lines",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_solutions",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_purchase_orders",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_vendors",
                schema: "crm");

            migrationBuilder.DropColumn(
                name: "PurchaseOrdersErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "PurchaseOrdersInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "PurchaseOrdersSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "PurchaseOrdersUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "SolutionsErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "SolutionsInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "SolutionsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "SolutionsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "VendorsErrored",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "VendorsInserted",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "VendorsSkipped",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "VendorsUpdated",
                schema: "crm",
                table: "crm_zoho_import_job");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                schema: "crm",
                table: "crm_orders");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                schema: "crm",
                table: "crm_invoices");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                schema: "crm",
                table: "crm_invoices");

            migrationBuilder.DropColumn(
                name: "ActivityType",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "CallDurationSeconds",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "CallType",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.DropColumn(
                name: "EventTitle",
                schema: "crm",
                table: "crm_activities");

            migrationBuilder.AlterColumn<string>(
                name: "Modules",
                schema: "crm",
                table: "crm_zoho_import_job",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
