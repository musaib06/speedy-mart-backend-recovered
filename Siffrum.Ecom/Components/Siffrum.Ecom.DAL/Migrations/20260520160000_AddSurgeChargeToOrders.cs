using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddSurgeChargeToOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- ══════ orders: surge_charge ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='surge_charge') THEN
                        ALTER TABLE orders ADD COLUMN surge_charge numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE orders DROP COLUMN IF EXISTS surge_charge;
            ");
        }
    }
}
