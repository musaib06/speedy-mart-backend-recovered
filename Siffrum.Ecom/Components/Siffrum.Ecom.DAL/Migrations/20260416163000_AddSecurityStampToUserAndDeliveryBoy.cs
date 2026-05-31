using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddSecurityStampToUserAndDeliveryBoy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe for production: IF NOT EXISTS prevents errors if column was added manually
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'users' AND column_name = 'security_stamp'
                    ) THEN
                        ALTER TABLE users ADD COLUMN security_stamp VARCHAR(36);
                    END IF;
                END
                $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'delivery_boys' AND column_name = 'security_stamp'
                    ) THEN
                        ALTER TABLE delivery_boys ADD COLUMN security_stamp VARCHAR(36);
                    END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE users DROP COLUMN IF EXISTS security_stamp;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE delivery_boys DROP COLUMN IF EXISTS security_stamp;
            ");
        }
    }
}
