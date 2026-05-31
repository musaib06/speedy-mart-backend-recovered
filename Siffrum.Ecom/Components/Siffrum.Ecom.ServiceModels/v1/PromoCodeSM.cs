using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class PromoCodeSM : SiffrumServiceModelBase<long>
    {
        public string Code { get; set; } // "SAVE10"

        public CouponTypeSM Type { get; set; }
        // Percentage or Flat

        public decimal DiscountValue { get; set; }
        // 10 (means 10% OR 10 currency depending on type)

        public decimal? MaxDiscountAmount { get; set; }
        // For percentage coupons (cap limit)

        public decimal? MinimumCartAmount { get; set; }

        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }

        public int? UsagePerUserLimit { get; set; }

        public bool IsActive { get; set; }

        public bool IsFirstOrderOnly { get; set; }

        public PlatformTypeSM PlatformType { get; set; }

        public DeliverySpeedTypeSM? ApplicableDeliverySpeed { get; set; }

    }

    public class PromoCodeValidateRequestSM
    {
        public string Code { get; set; }
        public decimal CartSubtotal { get; set; }
        public PlatformTypeSM? PlatformType { get; set; }
        public DeliverySpeedTypeSM? DeliverySpeedType { get; set; }
    }

    public class PromoCodeValidationResultSM
    {
        public bool IsValid { get; set; }
        public long PromoCodeId { get; set; }
        public string Code { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string Message { get; set; }
    }
}
