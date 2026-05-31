using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddDeliverySpeedToBanners : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- ══════ banners: delivery speed columns ══════
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='banners' AND column_name='is_normal') THEN
                        ALTER TABLE banners ADD COLUMN is_normal boolean NOT NULL DEFAULT true;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='banners' AND column_name='is_express') THEN
                        ALTER TABLE banners ADD COLUMN is_express boolean NOT NULL DEFAULT false;
                    END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE banners DROP COLUMN IF EXISTS is_normal;
                ALTER TABLE banners DROP COLUMN IF EXISTS is_express;
            ");
        }
    }
}
