using Siffrum.Ecom.ServiceModels.Enums;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class RemoveCartItemRequestSM
    {
        public long ProductVariantId { get; set; }
        public PlatformTypeSM PlatformType { get; set; }
        // SpeedyMart only: required when the same variant exists in both Express and Normal carts
        public DeliverySpeedTypeSM? DeliverySpeedType { get; set; }
    }
}
