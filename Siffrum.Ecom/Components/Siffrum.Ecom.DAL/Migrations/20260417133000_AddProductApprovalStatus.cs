using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddProductApprovalStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prod-safe: only add if not already present
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='approval_status'
                    ) THEN
                        ALTER TABLE products ADD COLUMN approval_status integer NOT NULL DEFAULT 2;
                    END IF;
                END
                $$;
            ");
            // Default 2 = Active (existing rows stay Active, seller-created ones will be set to 1 = PendingApproval by code)
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "products");
        }
    }
}
