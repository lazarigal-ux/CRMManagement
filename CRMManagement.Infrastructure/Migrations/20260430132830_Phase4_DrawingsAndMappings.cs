using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4_DrawingsAndMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crm_class_product_mappings",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Multiplier = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_class_product_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_crm_class_product_mappings_crm_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "crm",
                        principalTable: "crm_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "crm_drawing_analyses",
                schema: "crm",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Instruction = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ItemsJson = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AiLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crm_drawing_analyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_crm_class_product_mappings_Label",
                schema: "crm",
                table: "crm_class_product_mappings",
                column: "Label",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_crm_class_product_mappings_ProductId",
                schema: "crm",
                table: "crm_class_product_mappings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_drawing_analyses_AccountId",
                schema: "crm",
                table: "crm_drawing_analyses",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_drawing_analyses_OpportunityId",
                schema: "crm",
                table: "crm_drawing_analyses",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_crm_drawing_analyses_Status",
                schema: "crm",
                table: "crm_drawing_analyses",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_class_product_mappings",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "crm_drawing_analyses",
                schema: "crm");
        }
    }
}
