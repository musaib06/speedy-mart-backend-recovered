using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class SpeedyMartOfferSM : SiffrumServiceModelBase<long>
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OfferType { get; set; } = 1; // 1=Product, 2=Category, 3=Cart
        public int DiscountType { get; set; } = 1; // 1=Percentage, 2=Fixed, 3=BuyXGetY
        public decimal DiscountValue { get; set; }
        public DeliverySpeedTypeSM ApplicableDeliverySpeed { get; set; } = DeliverySpeedTypeSM.Both;
        public decimal? MinOrderValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public long? TargetId { get; set; }
        public string? TargetName { get; set; } // Product or category name for display
        public string? OfferCode { get; set; }
        public PlatformTypeSM PlatformType { get; set; } = PlatformTypeSM.SpeedyMart;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
