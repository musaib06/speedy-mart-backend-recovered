using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("low_stock_alerts")]
    public class LowStockAlertDM : SiffrumDomainModelBase<long>
    {
        [ForeignKey(nameof(ProductVariant))]
        [Column("product_variant_id")]
        public long ProductVariantId { get; set; }
        public virtual ProductVariantDM? ProductVariant { get; set; }

        [Column("seller_id")]
        public long SellerId { get; set; }

        [Column("threshold_quantity")]
        public int ThresholdQuantity { get; set; } = 5;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("last_alert_sent_at")]
        public DateTime? LastAlertSentAt { get; set; }
    }
}
