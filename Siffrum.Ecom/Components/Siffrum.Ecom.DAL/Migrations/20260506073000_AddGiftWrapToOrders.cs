using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddGiftWrapToOrders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='is_gift_wrap_included') THEN
                        ALTER TABLE orders ADD COLUMN is_gift_wrap_included boolean NOT NULL DEFAULT false;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='gift_wrap_charge') THEN
                        ALTER TABLE orders ADD COLUMN gift_wrap_charge numeric NOT NULL DEFAULT 0;
                    END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_gift_wrap_included",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "gift_wrap_charge",
                table: "orders");
        }
    }
}
