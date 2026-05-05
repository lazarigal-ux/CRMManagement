using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase13_AddSavedViewsTable : Migration
    {
        // Phase8_SavedViews shipped with empty Up()/Down() bodies, so existing databases
        // have it recorded in __EFMigrationsHistory but no crm_saved_views table exists.
        // This migration creates the table idempotently — fresh installs that ran the empty
        // Phase8 still get the table here, and any DB that somehow already created it via
        // EnsureCreated is left alone.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS crm.crm_saved_views (
    ""Id"" uuid NOT NULL,
    ""EntityType"" character varying(40) NOT NULL,
    ""Name"" character varying(150) NOT NULL,
    ""OwnerUserId"" uuid NULL,
    ""ViewMode"" character varying(20) NOT NULL,
    ""FiltersJson"" text NOT NULL,
    ""ColumnsJson"" text NULL,
    ""IsShared"" boolean NOT NULL,
    ""IsDefault"" boolean NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    ""CreatedByUserId"" uuid NULL,
    ""UpdatedByUserId"" uuid NULL,
    CONSTRAINT ""PK_crm_saved_views"" PRIMARY KEY (""Id"")
);

CREATE INDEX IF NOT EXISTS ""IX_crm_saved_views_EntityType_IsShared""
    ON crm.crm_saved_views (""EntityType"", ""IsShared"");

CREATE INDEX IF NOT EXISTS ""IX_crm_saved_views_EntityType_OwnerUserId""
    ON crm.crm_saved_views (""EntityType"", ""OwnerUserId"");
");

            // Permission repair lives in Phase14 (Phase14_RepairSavedViewsAccess) so this
            // migration stays purely DDL and doesn't trip on PG's GRANT-without-grant-option
            // warning behaviour when run by a non-owning role (e.g. dev ldataapp on a table
            // postgres created via EnsureCreated).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crm_saved_views",
                schema: "crm");
        }
    }
}
