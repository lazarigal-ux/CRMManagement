using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRMManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_RepairSavedViewsAccess : Migration
    {
        // Phase13 created crm.crm_saved_views with CREATE TABLE IF NOT EXISTS, which means
        // any DB where the table was already created out-of-band (typically by an earlier
        // EnsureCreated run while connected as postgres) kept its original ownership. The
        // runtime app role (ldataapp in dev) then hits "42501: permission denied for table
        // crm_saved_views" on every Leads/Contacts page load.
        //
        // This migration repairs that without ever raising an error itself: it only attempts
        // GRANT when the running role would actually be allowed to do so (it owns the table
        // or is a superuser). For DBs whose migrations run as ldataapp (a non-owner) the
        // operator must apply the same GRANT manually as postgres — see
        // scripts/fix-saved-views-permissions.sql.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    can_grant boolean;
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'crm' AND table_name = 'crm_saved_views'
    ) THEN
        RETURN;
    END IF;

    SELECT
        pg_catalog.pg_get_userbyid(c.relowner) = current_user
        OR EXISTS (SELECT 1 FROM pg_roles WHERE rolname = current_user AND rolsuper)
    INTO can_grant
    FROM pg_catalog.pg_class c
    JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
    WHERE n.nspname = 'crm' AND c.relname = 'crm_saved_views';

    IF NOT COALESCE(can_grant, false) THEN
        RETURN;
    END IF;

    EXECUTE format(
        'GRANT SELECT, INSERT, UPDATE, DELETE ON crm.crm_saved_views TO %I',
        current_user);

    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ldataapp') THEN
        EXECUTE 'GRANT USAGE ON SCHEMA crm TO ldataapp';
        EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON crm.crm_saved_views TO ldataapp';
    END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: this migration only widens permissions; reversing it on roll-back
            // would only re-introduce the bug it fixes.
        }
    }
}
