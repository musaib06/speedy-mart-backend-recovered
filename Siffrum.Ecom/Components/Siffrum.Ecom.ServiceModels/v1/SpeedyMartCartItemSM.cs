using Siffrum.Ecom.ServiceModels.Enums;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class SpeedyMartCartItemSM
    {
        public long Id { get; set; }
        public long CartId { get; set; }
        public long ProductVariantId { get; set; }
        public UserSpeedyMartProductSM? SpeedyMartProductDetails { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        // e.g. "1 kg", "500 ml" — variant name used as unit label
        public string? UnitLabel { get; set; }
        // 1=Normal, 2=Express (resolved at add-to-cart time)
        public DeliverySpeedTypeSM DeliverySpeedType { get; set; }
    }
}
