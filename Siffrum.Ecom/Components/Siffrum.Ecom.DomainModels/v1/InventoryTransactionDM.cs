using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("inventory_transactions")]
    [Index(nameof(ProductVariantId))]
    [Index(nameof(SellerId))]
    [Index(nameof(CreatedAt))]
    public class InventoryTransactionDM : SiffrumDomainModelBase<long>
    {
        [ForeignKey(nameof(ProductVariant))]
        [Column("product_variant_id")]
        public long ProductVariantId { get; set; }
        public virtual ProductVariantDM ProductVariant { get; set; }

        [Column("seller_id")]
        public long? SellerId { get; set; }

        [Required]
        [Column("change_type")]
        [MaxLength(50)]
        public string ChangeType { get; set; } = string.Empty; // "StockSet", "OrderDeducted", "OrderRefunded", "AdminAdjust"

        [Column("quantity_before")]
        public decimal? QuantityBefore { get; set; }

        [Column("quantity_after")]
        public decimal? QuantityAfter { get; set; }

        [Column("delta")]
        public decimal Delta { get; set; }

        [Column("reference_id")]
        public long? ReferenceId { get; set; } // OrderId if from order

        [Column("note")]
        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
