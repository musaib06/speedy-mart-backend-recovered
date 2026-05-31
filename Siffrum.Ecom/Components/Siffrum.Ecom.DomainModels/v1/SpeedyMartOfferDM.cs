using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("speedy_mart_offers")]
    public class SpeedyMartOfferDM : SiffrumDomainModelBase<long>
    {
        [Required]
        [Column("title")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Column("offer_type")]
        public int OfferType { get; set; } = 1; // 1=Product, 2=Category, 3=Cart

        [Column("discount_type")]
        public int DiscountType { get; set; } = 1; // 1=Percentage, 2=Fixed, 3=BuyXGetY

        [Column("discount_value")]
        public decimal DiscountValue { get; set; }

        [Column("applicable_delivery_speed")]
        public DeliverySpeedTypeDM ApplicableDeliverySpeed { get; set; } = DeliverySpeedTypeDM.Both;

        [Column("min_order_value")]
        public decimal? MinOrderValue { get; set; }

        [Column("max_discount")]
        public decimal? MaxDiscount { get; set; }

        [Column("target_id")]
        public long? TargetId { get; set; } // ProductId or CategoryId depending on OfferType

        [Column("offer_code")]
        [MaxLength(50)]
        public string? OfferCode { get; set; }

        [Column("platform_type")]
        public PlatformTypeDM PlatformType { get; set; } = PlatformTypeDM.SpeedyMart;

        [Column("valid_from")]
        public DateTime? ValidFrom { get; set; }

        [Column("valid_to")]
        public DateTime? ValidTo { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
