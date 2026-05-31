using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class FixActivityLogsColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop and recreate created_by as varchar to match DomainModelRoot string type
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='created_by') THEN
                        ALTER TABLE activity_logs DROP COLUMN created_by;
                    END IF;
                    ALTER TABLE activity_logs ADD COLUMN created_by varchar(100) NULL;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='updated_at') THEN
                        ALTER TABLE activity_logs ADD COLUMN updated_at timestamp NULL;
                    END IF;

                    -- Drop and recreate updated_by as varchar to match DomainModelRoot string type
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='updated_by') THEN
                        ALTER TABLE activity_logs DROP COLUMN updated_by;
                    END IF;
                    ALTER TABLE activity_logs ADD COLUMN updated_by varchar(100) NULL;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='deleted_at') THEN
                        ALTER TABLE activity_logs ADD COLUMN deleted_at timestamp NULL;
                    END IF;

                    -- Drop and recreate deleted_by as varchar to match DomainModelRoot string type
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='deleted_by') THEN
                        ALTER TABLE activity_logs DROP COLUMN deleted_by;
                    END IF;
                    ALTER TABLE activity_logs ADD COLUMN deleted_by varchar(100) NULL;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No down migration needed
        }
    }
}
