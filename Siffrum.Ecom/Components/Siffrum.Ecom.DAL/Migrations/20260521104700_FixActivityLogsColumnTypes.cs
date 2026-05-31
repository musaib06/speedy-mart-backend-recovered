using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class FixActivityLogsColumnTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Fix created_by from bigint to varchar
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='created_by' AND data_type='bigint') THEN
                        ALTER TABLE activity_logs DROP COLUMN created_by;
                        ALTER TABLE activity_logs ADD COLUMN created_by varchar(100) NULL;
                    END IF;

                    -- Fix updated_by from bigint to varchar
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='updated_by' AND data_type='bigint') THEN
                        ALTER TABLE activity_logs DROP COLUMN updated_by;
                        ALTER TABLE activity_logs ADD COLUMN updated_by varchar(100) NULL;
                    END IF;

                    -- Fix deleted_by from bigint to varchar
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='deleted_by' AND data_type='bigint') THEN
                        ALTER TABLE activity_logs DROP COLUMN deleted_by;
                        ALTER TABLE activity_logs ADD COLUMN deleted_by varchar(100) NULL;
                    END IF;
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
