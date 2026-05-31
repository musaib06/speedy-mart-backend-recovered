using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.Foundation.Base;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("product_variants")]
    [Index(nameof(ProductId), nameof(PlatformType))]
    [Index(nameof(PlatformType))]
    [Index(nameof(Status))]
    public class ProductVariantDM : SiffrumDomainModelBase<long>
    {
        [Column("name")]
        public string Name { get; set; }

        [Column("indicator")]
        public ProductIndicatorDM Indicator { get; set; }

        [Column("manufacturer")]
        [MaxLength(191)]
        public string? Manufacturer { get; set; }

        [Column("made_in")]
        [MaxLength(191)]
        public string? MadeIn { get; set; }

        [Column("is_cancelable")]
        public bool IsCancelable { get; set; }

        [Column("image")]
        public string? Image { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("status")]
        public ProductStatusDM Status { get; set; } = 0;

        [Column("platform_type")]
        public PlatformTypeDM PlatformType { get; set; }

        [Column("return_days")]
        public int ReturnDays { get; set; }


        [Column("is_unlimited_stock")]
        public bool IsUnlimitedStock { get; set; }

        [Column("is_cod_allowed")]
        public bool IsCodAllowed { get; set; }

        [Column("fssai_lic_no")]
        [MaxLength(191)]
        public string? FssaiLicNo { get; set; } 

        [Column("barcode")]
        public string? Barcode { get; set; }

        [Column("meta_title")]
        [MaxLength(191)]
        public string? MetaTitle { get; set; }

        [Column("meta_keywords")]
        public string? MetaKeywords { get; set; }

        [Column("schema_markup")]
        public string? SchemaMarkup { get; set; }

        [Column("meta_description")]
        public string? MetaDescription { get; set; }

        [Column("total_allowed_quantity")]
        public int? TotalAllowedQuantity { get; set; }

        [Column("is_tax_included_in_price")]
        public bool IsTaxIncludedInPrice { get; set; }

        [Column("return_policy")]
        public ProductReturnPolicyDM ReturnPolicy { get; set; }


        [Column("measurement")]
        public string? Measurement { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        [Column("discounted_price")]
        public decimal? DiscountedPrice { get; set; } 

        [Column("stock")]
        public decimal? Stock { get; set; }
        [Column("view_count")]
        public int? ViewCount { get; set; } = 0;

        [Column("sku")]
        public string SKU { get; set; } = string.Empty;

        // SpeedyMart-specific fields for variants
        [Column("compare_at_price")]
        public decimal? CompareAtPrice { get; set; }  // Original "crossed out" price (strikethrough)

        [Column("variant_attributes")]
        public string? VariantAttributes { get; set; }  // JSON: {"color": "Blue", "size": "Large"}

        [Column("delivery_speed_type")]
        public int DeliverySpeedType { get; set; } = 1;  // 1=NormalOnly, 2=ExpressOnly, 3=ExpressAndNormal

        [Column("variant_image_url")]
        [MaxLength(500)]
        public string? VariantImageUrl { get; set; }  // Per-variant image (e.g. color-specific)

        // Produce-specific convenience fields
        [Column("sell_by_mode")] // 1=Piece, 2=Weight
        public int SellByMode { get; set; } = 1;

        [Column("min_order_qty")]
        public decimal? MinOrderQty { get; set; }

        [Column("max_order_qty")]
        public decimal? MaxOrderQty { get; set; }

        [Column("order_step_qty")]
        public decimal? OrderStepQty { get; set; }

        [Column("shelf_life_days")]
        public int? ShelfLifeDays { get; set; }

        [Column("best_before_label")]
        public string? BestBeforeLabel { get; set; }

        [Column("is_organic")]
        public bool IsOrganic { get; set; }

        [Column("requires_cold_chain")]
        public bool RequiresColdChain { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [ForeignKey(nameof(Product))]
        [Required]
        [Column("product_id")]
        public long ProductId { get; set; }
        public virtual ProductDM Product { get; set; }
        public ICollection<ProductImagesDM> Images { get; set; }
        public virtual ICollection<AddOnProductsDM> MainProductAddons { get; set; }
        public virtual ICollection<AddOnProductsDM> AddonProducts { get; set; }
        public ICollection<ProductFaqDM> ProductFaqs { get; set; }
        public ICollection<ProductRatingDM> Ratings { get; set; }
        public ICollection<ProductSpecificationDM> ProductSpecifications { get; set; }
        public ICollection<ProductNutritionDataDM> NutritionValues { get; set; }
        public ICollection<ProductBannerDM> BannerProducts { get; set; }
        public ICollection<ProductSpecificationFilterValueDM> ProductSpecificationFilter { get; set; }
        public ICollection<OrderItemDM> Items { get; set; } 
    }
}
