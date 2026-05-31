using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siffrum.Ecom.DAL.Migrations
{
    /// <summary>
    /// Consolidated SpeedyMart schema migration — merges all unpushed migrations:
    ///   - AddDeliverySpeedTypeToCategory
    ///   - AddPlatformToTag
    ///   - AddSpeedyMartProductEnhancements (variant_attributes, delivery_speed_type, spec fields)
    ///   - AddCompareAtPriceToVariant
    ///   - AddCategorySpecTemplates (new table)
    ///   - AddCategoryAttrDimensions (new table)
    ///   - AddInventoryTransactions (new table)
    ///   - AddDeliverySpeedTypeToCartItems
    ///   - AddSpeedyMartMissingSchema (variant fields, products, promocodes, tables)
    ///   - AddPlatformTypeToStoreHours (new)
    /// All statements are idempotent (IF NOT EXISTS / CREATE IF NOT EXISTS).
    /// </summary>
    public partial class SpeedyMartFullSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══════════════════════════════════════════
            //  1. CATEGORIES — delivery_speed_type, is_express_eligible
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='categories' AND column_name='delivery_speed_type') THEN
                        ALTER TABLE categories ADD COLUMN delivery_speed_type integer NOT NULL DEFAULT 1;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='categories' AND column_name='is_express_eligible') THEN
                        ALTER TABLE categories ADD COLUMN is_express_eligible BOOLEAN DEFAULT FALSE;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  2. TAGS — platform
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='tags' AND column_name='platform') THEN
                        ALTER TABLE tags ADD COLUMN platform integer NOT NULL DEFAULT 1;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  3. PRODUCT_VARIANTS — all new columns
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='variant_attributes') THEN
                        ALTER TABLE product_variants ADD COLUMN variant_attributes text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='delivery_speed_type') THEN
                        ALTER TABLE product_variants ADD COLUMN delivery_speed_type integer NOT NULL DEFAULT 1;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='compare_at_price') THEN
                        ALTER TABLE product_variants ADD COLUMN compare_at_price numeric(18,2) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='variant_image_url') THEN
                        ALTER TABLE product_variants ADD COLUMN variant_image_url VARCHAR(500) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='sell_by_mode') THEN
                        ALTER TABLE product_variants ADD COLUMN sell_by_mode INT NOT NULL DEFAULT 1;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='min_order_qty') THEN
                        ALTER TABLE product_variants ADD COLUMN min_order_qty DECIMAL NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='max_order_qty') THEN
                        ALTER TABLE product_variants ADD COLUMN max_order_qty DECIMAL NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='order_step_qty') THEN
                        ALTER TABLE product_variants ADD COLUMN order_step_qty DECIMAL NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='shelf_life_days') THEN
                        ALTER TABLE product_variants ADD COLUMN shelf_life_days INT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='best_before_label') THEN
                        ALTER TABLE product_variants ADD COLUMN best_before_label VARCHAR(200) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='is_organic') THEN
                        ALTER TABLE product_variants ADD COLUMN is_organic BOOLEAN NOT NULL DEFAULT FALSE;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_variants' AND column_name='requires_cold_chain') THEN
                        ALTER TABLE product_variants ADD COLUMN requires_cold_chain BOOLEAN NOT NULL DEFAULT FALSE;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  4. PRODUCT_SPECIFICATIONS — spec enhancements
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_specifications' AND column_name='specification_group') THEN
                        ALTER TABLE product_specifications ADD COLUMN specification_group character varying(50) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_specifications' AND column_name='display_order') THEN
                        ALTER TABLE product_specifications ADD COLUMN display_order integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_specifications' AND column_name='is_filterable') THEN
                        ALTER TABLE product_specifications ADD COLUMN is_filterable boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  5. PRODUCTS — overview_points, approval workflow
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='overview_points') THEN
                        ALTER TABLE products ADD COLUMN overview_points JSONB NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='submitted_by_seller_id') THEN
                        ALTER TABLE products ADD COLUMN submitted_by_seller_id BIGINT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='submitted_at') THEN
                        ALTER TABLE products ADD COLUMN submitted_at TIMESTAMP NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='approved_by_admin_id') THEN
                        ALTER TABLE products ADD COLUMN approved_by_admin_id BIGINT NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='approved_at') THEN
                        ALTER TABLE products ADD COLUMN approved_at TIMESTAMP NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='products' AND column_name='rejection_reason') THEN
                        ALTER TABLE products ADD COLUMN rejection_reason TEXT NULL;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  6. PROMOCODES — platform_type, applicable_delivery_speed
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='promo_codes' AND column_name='platform_type') THEN
                        ALTER TABLE promo_codes ADD COLUMN platform_type INT NOT NULL DEFAULT 1;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='promo_codes' AND column_name='applicable_delivery_speed') THEN
                        ALTER TABLE promo_codes ADD COLUMN applicable_delivery_speed INT NULL;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  6b. ORDERS — platform_type
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='orders' AND column_name='platform_type') THEN
                        ALTER TABLE orders ADD COLUMN platform_type integer NOT NULL DEFAULT 1;
                    END IF;
                END $$;
            ");
            // Backfill: existing orders default to HotBox (1) which is correct

            // ═══════════════════════════════════════════
            //  7. CART_ITEMS — delivery_speed_type
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='cart_items' AND column_name='delivery_speed_type') THEN
                        ALTER TABLE cart_items ADD COLUMN delivery_speed_type integer NULL;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  8. STORE_HOURS — platform_type + update unique index
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='store_hours' AND column_name='platform_type') THEN
                        ALTER TABLE store_hours ADD COLUMN platform_type smallint NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");
            // Drop old unique index, create new one with platform_type
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_store_hours_seller_id_day_of_week"";
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_store_hours_seller_id_day_of_week_platform_type""
                    ON store_hours (seller_id, day_of_week, platform_type);
            ");
            // Migrate existing rows to HotBox (1) since they were created from the HotBox Store Hours page
            migrationBuilder.Sql(@"UPDATE store_hours SET platform_type = 1 WHERE platform_type = 0;");

            // ═══════════════════════════════════════════
            //  9. NEW TABLE — category_spec_templates
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS category_spec_templates (
                    id BIGSERIAL PRIMARY KEY,
                    category_id BIGINT NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
                    spec_key VARCHAR(100) NOT NULL,
                    spec_label VARCHAR(100) NOT NULL,
                    spec_group VARCHAR(50) NULL,
                    placeholder VARCHAR(200) NULL,
                    is_required BOOLEAN NOT NULL DEFAULT FALSE,
                    display_order INT NOT NULL DEFAULT 0,
                    created_at TIMESTAMP NULL,
                    updated_at TIMESTAMP NULL,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL,
                    deleted_at TIMESTAMP NULL
                );
                CREATE INDEX IF NOT EXISTS idx_cat_spec_tmpl_category ON category_spec_templates(category_id);
            ");

            // ═══════════════════════════════════════════
            //  10. NEW TABLE — category_attr_dimensions
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS category_attr_dimensions (
                    id BIGSERIAL PRIMARY KEY,
                    category_id BIGINT NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
                    name VARCHAR(100) NOT NULL,
                    values_json TEXT NULL,
                    is_required BOOLEAN NOT NULL DEFAULT FALSE,
                    display_order INT NOT NULL DEFAULT 0,
                    created_at TIMESTAMP NULL,
                    updated_at TIMESTAMP NULL,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL
                );
                CREATE INDEX IF NOT EXISTS idx_cat_attr_dim_category ON category_attr_dimensions(category_id);
            ");

            // ═══════════════════════════════════════════
            //  11. NEW TABLE — inventory_transactions
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS inventory_transactions (
                    id BIGSERIAL PRIMARY KEY,
                    product_variant_id BIGINT NOT NULL REFERENCES product_variants(id) ON DELETE CASCADE,
                    seller_id BIGINT NULL,
                    change_type VARCHAR(50) NOT NULL,
                    quantity_before NUMERIC(18,2) NULL,
                    quantity_after NUMERIC(18,2) NULL,
                    delta NUMERIC(18,2) NOT NULL DEFAULT 0,
                    reference_id BIGINT NULL,
                    note VARCHAR(255) NULL,
                    created_at TIMESTAMP NULL,
                    updated_at TIMESTAMP NULL,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL
                );
                CREATE INDEX IF NOT EXISTS idx_inv_tx_variant ON inventory_transactions(product_variant_id);
                CREATE INDEX IF NOT EXISTS idx_inv_tx_seller ON inventory_transactions(seller_id);
                CREATE INDEX IF NOT EXISTS idx_inv_tx_created ON inventory_transactions(created_at);
            ");

            // ═══════════════════════════════════════════
            //  12. NEW TABLE — product_attribute_dimensions
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS product_attribute_dimensions (
                    id BIGSERIAL PRIMARY KEY,
                    product_id BIGINT NOT NULL,
                    dimension_key VARCHAR(100) NOT NULL,
                    dimension_label VARCHAR(100) NOT NULL,
                    display_type VARCHAR(20) DEFAULT 'button',
                    display_order INT DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL,
                    UNIQUE(product_id, dimension_key)
                );
                CREATE INDEX IF NOT EXISTS idx_attr_dim_product ON product_attribute_dimensions(product_id);
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='product_attribute_dimensions' AND column_name='values_json') THEN
                        ALTER TABLE product_attribute_dimensions ADD COLUMN values_json JSONB NULL;
                    END IF;
                END $$;
            ");

            // ═══════════════════════════════════════════
            //  13. NEW TABLE — low_stock_alerts
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS low_stock_alerts (
                    id BIGSERIAL PRIMARY KEY,
                    product_variant_id BIGINT NOT NULL,
                    seller_id BIGINT NOT NULL,
                    threshold_quantity INT NOT NULL DEFAULT 5,
                    is_active BOOLEAN DEFAULT TRUE,
                    last_alert_sent_at TIMESTAMP NULL,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL
                );
                CREATE INDEX IF NOT EXISTS idx_low_stock_variant ON low_stock_alerts(product_variant_id);
                CREATE INDEX IF NOT EXISTS idx_low_stock_seller ON low_stock_alerts(seller_id, is_active);
            ");

            // ═══════════════════════════════════════════
            //  14. NEW TABLE — speedy_mart_offers
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS speedy_mart_offers (
                    id BIGSERIAL PRIMARY KEY,
                    title VARCHAR(200) NOT NULL,
                    description TEXT NULL,
                    offer_type INT NOT NULL DEFAULT 1,
                    discount_type INT NOT NULL DEFAULT 1,
                    discount_value DECIMAL(10,2) NOT NULL,
                    applicable_delivery_speed INT DEFAULT 3,
                    min_order_value DECIMAL(10,2) NULL,
                    max_discount DECIMAL(10,2) NULL,
                    target_id BIGINT NULL,
                    offer_code VARCHAR(50) NULL,
                    platform_type INT NOT NULL DEFAULT 2,
                    valid_from TIMESTAMP NULL,
                    valid_to TIMESTAMP NULL,
                    is_active BOOLEAN DEFAULT TRUE,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL,
                    CONSTRAINT check_platform_speedymart CHECK (platform_type = 2)
                );
                CREATE INDEX IF NOT EXISTS idx_sm_offers_active ON speedy_mart_offers(is_active, valid_from, valid_to);
                CREATE INDEX IF NOT EXISTS idx_sm_offers_code ON speedy_mart_offers(offer_code) WHERE offer_code IS NOT NULL;
            ");

            // ═══════════════════════════════════════════
            //  15. NEW TABLE — product_complaints
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS product_complaints (
                    id BIGSERIAL PRIMARY KEY,
                    product_id BIGINT NOT NULL,
                    seller_id BIGINT NOT NULL,
                    complaint_type INT NOT NULL,
                    subject VARCHAR(200) NOT NULL,
                    description TEXT NOT NULL,
                    attachments JSONB DEFAULT '[]',
                    status INT DEFAULT 1,
                    priority INT DEFAULT 2,
                    assigned_to_admin_id BIGINT NULL,
                    resolution_notes TEXT NULL,
                    resolved_by_admin_id BIGINT NULL,
                    resolved_at TIMESTAMP NULL,
                    platform_type INT NOT NULL DEFAULT 2,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_by VARCHAR(100) NULL,
                    updated_by VARCHAR(100) NULL
                );
                CREATE INDEX IF NOT EXISTS idx_complaint_seller ON product_complaints(seller_id, status);
                CREATE INDEX IF NOT EXISTS idx_complaint_product ON product_complaints(product_id);
                CREATE INDEX IF NOT EXISTS idx_complaint_status ON product_complaints(status, priority, created_at);
            ");

            // ═══════════════════════════════════════════
            //  16. NEW TABLE — product_complaint_comments
            // ═══════════════════════════════════════════
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS product_complaint_comments (
                    id BIGSERIAL PRIMARY KEY,
                    complaint_id BIGINT NOT NULL,
                    commenter_type INT NOT NULL,
                    commenter_id BIGINT NOT NULL,
                    comment TEXT NOT NULL,
                    attachments JSONB DEFAULT '[]',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_by VARCHAR(100) NULL,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_by VARCHAR(100) NULL
                );
                CREATE INDEX IF NOT EXISTS idx_complaint_comments ON product_complaint_comments(complaint_id, created_at);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Tables (reverse order)
            migrationBuilder.Sql("DROP TABLE IF EXISTS product_complaint_comments;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS product_complaints;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS speedy_mart_offers;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS low_stock_alerts;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS product_attribute_dimensions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS inventory_transactions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS category_attr_dimensions;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS category_spec_templates;");

            // Store hours — revert index and drop column
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ""IX_store_hours_seller_id_day_of_week_platform_type"";
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_store_hours_seller_id_day_of_week""
                    ON store_hours (seller_id, day_of_week);
            ");
            migrationBuilder.DropColumn(name: "platform_type", table: "store_hours");

            // Cart items
            migrationBuilder.DropColumn(name: "delivery_speed_type", table: "cart_items");

            // Orders
            migrationBuilder.Sql("ALTER TABLE orders DROP COLUMN IF EXISTS platform_type;");

            // Promocodes
            migrationBuilder.Sql("ALTER TABLE promo_codes DROP COLUMN IF EXISTS applicable_delivery_speed;");
            migrationBuilder.Sql("ALTER TABLE promo_codes DROP COLUMN IF EXISTS platform_type;");

            // Products
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS rejection_reason;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS approved_at;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS approved_by_admin_id;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS submitted_at;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS submitted_by_seller_id;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS overview_points;");

            // Product specifications
            migrationBuilder.DropColumn(name: "is_filterable", table: "product_specifications");
            migrationBuilder.DropColumn(name: "display_order", table: "product_specifications");
            migrationBuilder.DropColumn(name: "specification_group", table: "product_specifications");

            // Product variants
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS requires_cold_chain;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS is_organic;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS best_before_label;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS shelf_life_days;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS order_step_qty;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS max_order_qty;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS min_order_qty;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS sell_by_mode;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS variant_image_url;");
            migrationBuilder.Sql("ALTER TABLE product_variants DROP COLUMN IF EXISTS compare_at_price;");
            migrationBuilder.DropColumn(name: "delivery_speed_type", table: "product_variants");
            migrationBuilder.DropColumn(name: "variant_attributes", table: "product_variants");

            // Tags
            migrationBuilder.DropColumn(name: "platform", table: "tags");

            // Categories
            migrationBuilder.Sql("ALTER TABLE categories DROP COLUMN IF EXISTS is_express_eligible;");
            migrationBuilder.DropColumn(name: "delivery_speed_type", table: "categories");
        }
    }
}
