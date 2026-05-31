using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddWishlistItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS wishlist_items (
                    id              bigserial       PRIMARY KEY,
                    user_id         bigint          NOT NULL,
                    product_variant_id bigint       NOT NULL,
                    created_at      timestamp       NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
                    CONSTRAINT fk_wishlist_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                    CONSTRAINT fk_wishlist_variant FOREIGN KEY (product_variant_id) REFERENCES product_variants(id) ON DELETE CASCADE,
                    CONSTRAINT uq_wishlist_user_variant UNIQUE (user_id, product_variant_id)
                );

                CREATE INDEX IF NOT EXISTS ix_wishlist_items_user_id ON wishlist_items(user_id);
                CREATE INDEX IF NOT EXISTS ix_wishlist_items_variant_id ON wishlist_items(product_variant_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS wishlist_items;");
        }
    }
}
