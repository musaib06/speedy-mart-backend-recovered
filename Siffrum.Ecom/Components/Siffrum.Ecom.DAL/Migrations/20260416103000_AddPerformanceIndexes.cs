using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- ══════ users: login/OTP lookups ══════
                CREATE INDEX IF NOT EXISTS ""IX_users_mobile""
                    ON users (mobile);
                CREATE INDEX IF NOT EXISTS ""IX_users_email""
                    ON users (email);
                CREATE INDEX IF NOT EXISTS ""IX_users_username""
                    ON users (username);

                -- ══════ orders: seller dashboard, status filters ══════
                CREATE INDEX IF NOT EXISTS ""IX_orders_seller_id""
                    ON orders (seller_id);
                CREATE INDEX IF NOT EXISTS ""IX_orders_order_status""
                    ON orders (order_status);
                CREATE INDEX IF NOT EXISTS ""IX_orders_user_id_order_status""
                    ON orders (user_id, order_status);

                -- ══════ product_variants: platform & status filtering ══════
                CREATE INDEX IF NOT EXISTS ""IX_product_variants_product_id_platform_type""
                    ON product_variants (product_id, platform_type);
                CREATE INDEX IF NOT EXISTS ""IX_product_variants_platform_type""
                    ON product_variants (platform_type);
                CREATE INDEX IF NOT EXISTS ""IX_product_variants_status""
                    ON product_variants (status);

                -- ══════ categories: platform & status filtering ══════
                CREATE INDEX IF NOT EXISTS ""IX_categories_platform""
                    ON categories (platform);
                CREATE INDEX IF NOT EXISTS ""IX_categories_status""
                    ON categories (status);

                -- ══════ in_app_notifications: notification fetches ══════
                CREATE INDEX IF NOT EXISTS ""IX_in_app_notifications_recipient_type_recipient_id""
                    ON in_app_notifications (recipient_type, recipient_id);
                CREATE INDEX IF NOT EXISTS ""IX_in_app_notifications_recipient_id_is_read""
                    ON in_app_notifications (recipient_id, is_read);

                -- ══════ deliveries: order tracking, rider assignments ══════
                CREATE INDEX IF NOT EXISTS ""IX_deliveries_order_id""
                    ON deliveries (order_id);
                CREATE INDEX IF NOT EXISTS ""IX_deliveries_delivery_boy_id""
                    ON deliveries (delivery_boy_id);
                CREATE INDEX IF NOT EXISTS ""IX_deliveries_delivery_boy_id_status""
                    ON deliveries (delivery_boy_id, status);

                -- ══════ delivery_boy_order_transactions: ledger summary ══════
                CREATE INDEX IF NOT EXISTS ""IX_delivery_boy_order_transactions_dboy_paytype""
                    ON delivery_boy_order_transactions (delivery_boy_id, payment_type);

                -- ══════ DeliveryRequest: duplicate check, platform filter ══════
                CREATE INDEX IF NOT EXISTS ""IX_DeliveryRequest_user_id_is_resolved""
                    ON ""DeliveryRequest"" (user_id, is_resolved);
                CREATE INDEX IF NOT EXISTS ""IX_DeliveryRequest_platform""
                    ON ""DeliveryRequest"" (platform);

                -- ══════ delivery_places: availability check ══════
                CREATE INDEX IF NOT EXISTS ""IX_delivery_places_seller_pincode""
                    ON delivery_places (""sellerId"", pincode);
                CREATE INDEX IF NOT EXISTS ""IX_delivery_places_pincode""
                    ON delivery_places (pincode);

                -- ══════ cash_collections: seller cash ledger ══════
                CREATE INDEX IF NOT EXISTS ""IX_cash_collections_seller_id""
                    ON cash_collections (seller_id);
                CREATE INDEX IF NOT EXISTS ""IX_cash_collections_seller_dboy""
                    ON cash_collections (seller_id, delivery_boy_id);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_users_mobile"";
                DROP INDEX IF EXISTS ""IX_users_email"";
                DROP INDEX IF EXISTS ""IX_users_username"";
                DROP INDEX IF EXISTS ""IX_orders_seller_id"";
                DROP INDEX IF EXISTS ""IX_orders_order_status"";
                DROP INDEX IF EXISTS ""IX_orders_user_id_order_status"";
                DROP INDEX IF EXISTS ""IX_product_variants_product_id_platform_type"";
                DROP INDEX IF EXISTS ""IX_product_variants_platform_type"";
                DROP INDEX IF EXISTS ""IX_product_variants_status"";
                DROP INDEX IF EXISTS ""IX_categories_platform"";
                DROP INDEX IF EXISTS ""IX_categories_status"";
                DROP INDEX IF EXISTS ""IX_in_app_notifications_recipient_type_recipient_id"";
                DROP INDEX IF EXISTS ""IX_in_app_notifications_recipient_id_is_read"";
                DROP INDEX IF EXISTS ""IX_deliveries_order_id"";
                DROP INDEX IF EXISTS ""IX_deliveries_delivery_boy_id"";
                DROP INDEX IF EXISTS ""IX_deliveries_delivery_boy_id_status"";
                DROP INDEX IF EXISTS ""IX_delivery_boy_order_transactions_dboy_paytype"";
                DROP INDEX IF EXISTS ""IX_DeliveryRequest_user_id_is_resolved"";
                DROP INDEX IF EXISTS ""IX_DeliveryRequest_platform"";
                DROP INDEX IF EXISTS ""IX_delivery_places_seller_pincode"";
                DROP INDEX IF EXISTS ""IX_delivery_places_pincode"";
                DROP INDEX IF EXISTS ""IX_cash_collections_seller_id"";
                DROP INDEX IF EXISTS ""IX_cash_collections_seller_dboy"";
            ");
        }
    }
}
