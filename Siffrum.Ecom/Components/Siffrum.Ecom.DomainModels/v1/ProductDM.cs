using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("products")]
    [Index(nameof(SellerId), nameof(Slug), IsUnique = true)]
    [Index(nameof(SellerId))]
    [Index(nameof(CategoryId))]
    public class ProductDM : SiffrumDomainModelBase<long>
    {

        [Required]
        [Column("name")]
        [MaxLength(191)]
        public string Name { get; set; }                 

        [Required]
        [Column("slug")]
        [MaxLength(191)]
        public string Slug { get; set; }    

        // Relationships
        [ForeignKey(nameof(Seller))]
        [Required]
        [Column("seller_id")]
        public long SellerId { get; set; }
        public virtual SellerDM? Seller { get; set; }
        [ForeignKey(nameof(Category))]
        [Required]
        [Column("category_id")]
        public long CategoryId { get; set; }
        public virtual CategoryDM? Category { get; set; }
        [ForeignKey(nameof(Brand))]
        [Column("brand_id")]
        public long? BrandId { get; set; }
        public virtual BrandDM? Brand { get; set; }
        
        [Column("tax_percentage")]
        public decimal? TaxPercentage { get; set; }

        [Column("tags")]
        [MaxLength(500)]
        public string? Tags { get; set; }

        [Column("approval_status")]
        public ProductStatusDM ApprovalStatus { get; set; } = ProductStatusDM.Active;

        [Column("submitted_by_seller_id")]
        public long? SubmittedBySellerIdRaw { get; set; }

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [Column("approved_by_admin_id")]
        public long? ApprovedByAdminIdRaw { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        // SpeedyMart-specific: product overview as JSON (array of strings)
        [Column("overview_points", TypeName = "jsonb")]
        public string? OverviewPoints { get; set; }

        public ICollection<ProductVariantDM> ProductVariants { get; set; } 

    }
}
