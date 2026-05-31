using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddSpeedyMartApiGapFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- ══════ orders: delivery_speed_type ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='delivery_speed_type') THEN
                        ALTER TABLE orders ADD COLUMN delivery_speed_type integer NOT NULL DEFAULT 0;
                    END IF;

                    -- ══════ orders: discount_amount ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='discount_amount') THEN
                        ALTER TABLE orders ADD COLUMN discount_amount numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;

                    -- ══════ orders: promo_code_id ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='orders' AND column_name='promo_code_id') THEN
                        ALTER TABLE orders ADD COLUMN promo_code_id bigint NULL;
                    END IF;

                    -- ══════ delivery_places: platform_charges ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='delivery_places' AND column_name='platform_charges') THEN
                        ALTER TABLE delivery_places ADD COLUMN platform_charges numeric NOT NULL DEFAULT 0;
                    END IF;

                    -- ══════ delivery_places: free_delivery_threshold ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='delivery_places' AND column_name='free_delivery_threshold') THEN
                        ALTER TABLE delivery_places ADD COLUMN free_delivery_threshold numeric NOT NULL DEFAULT 0;
                    END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE orders DROP COLUMN IF EXISTS delivery_speed_type;
                ALTER TABLE orders DROP COLUMN IF EXISTS discount_amount;
                ALTER TABLE orders DROP COLUMN IF EXISTS promo_code_id;
                ALTER TABLE delivery_places DROP COLUMN IF EXISTS platform_charges;
                ALTER TABLE delivery_places DROP COLUMN IF EXISTS free_delivery_threshold;
            ");
        }
    }
}
