-- One-shot repair for "42501: permission denied for table crm_saved_views".
--
-- Cause: crm.crm_saved_views was originally created by a different role (typically
-- postgres) while the running app connects as ldataapp. Phase13's CREATE TABLE
-- IF NOT EXISTS was a no-op so ownership stayed with the original creator and
-- ldataapp got no grants on the table.
--
-- Run this script ONCE as a privileged role (postgres) against the same
-- database the app uses (ldatabrain). After this runs, Phase14_RepairSavedViewsAccess
-- prevents the bug from coming back on subsequent migrations.
--
--   psql -h localhost -U postgres -d ldatabrain -f scripts/fix-saved-views-permissions.sql

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ldataapp') THEN
        RAISE NOTICE 'Role "ldataapp" does not exist on this server; nothing to grant.';
        RETURN;
    END IF;

    GRANT USAGE ON SCHEMA crm TO ldataapp;

    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'crm' AND table_name = 'crm_saved_views'
    ) THEN
        -- Reassign ownership so subsequent migrations (e.g. Phase13's CREATE INDEX IF NOT
        -- EXISTS) run as the ldataapp role without tripping PG's ownership check, which
        -- happens before the IF NOT EXISTS clause.
        ALTER TABLE crm.crm_saved_views OWNER TO ldataapp;
        GRANT SELECT, INSERT, UPDATE, DELETE ON crm.crm_saved_views TO ldataapp;
        RAISE NOTICE 'Reassigned crm.crm_saved_views to ldataapp and granted CRUD.';
    ELSE
        RAISE NOTICE 'Table crm.crm_saved_views does not exist; run app once to apply migrations first.';
    END IF;

    -- Cover any other crm.* tables that ended up in the same bucket so this single
    -- repair handles the whole schema, not just one table.
    EXECUTE 'GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA crm TO ldataapp';
    EXECUTE 'GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA crm TO ldataapp';

    -- Future tables created in this schema by the current role will auto-grant the
    -- same access to ldataapp, so a recurrence of the bug is prevented.
    EXECUTE 'ALTER DEFAULT PRIVILEGES IN SCHEMA crm GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ldataapp';
    EXECUTE 'ALTER DEFAULT PRIVILEGES IN SCHEMA crm GRANT USAGE, SELECT ON SEQUENCES TO ldataapp';
END $$;
