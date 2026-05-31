using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class FixActivityLogsJsonColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Fix old_values from jsonb to text
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='old_values' AND data_type='jsonb') THEN
                        ALTER TABLE activity_logs ALTER COLUMN old_values TYPE text;
                    END IF;

                    -- Fix new_values from jsonb to text
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='activity_logs' AND column_name='new_values' AND data_type='jsonb') THEN
                        ALTER TABLE activity_logs ALTER COLUMN new_values TYPE text;
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
