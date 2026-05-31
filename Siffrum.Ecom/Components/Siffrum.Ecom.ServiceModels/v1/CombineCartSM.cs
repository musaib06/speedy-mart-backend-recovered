using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class CombineCartSM : SiffrumServiceModelBase<long>
    {
        public CartSM? HotBoxCart { get; set; }
        public CartSM? SpeedyMartCart { get; set; }

        public List<HotBoxCartItemSM>? HotBoxCartItems { get; set; }

        // Full SpeedyMart list (all speeds combined)
        public List<SpeedyMartCartItemSM>? SpeedyMartCartItems { get; set; }

        // SpeedyMart split by delivery speed — populated by BuildCombineCartResponse
        public List<SpeedyMartCartItemSM>? SpeedyMartExpressItems { get; set; }
        public List<SpeedyMartCartItemSM>? SpeedyMartNormalItems { get; set; }

        // Convenience totals for the dual-cart tabs
        public decimal SpeedyMartExpressSubTotal { get; set; }
        public decimal SpeedyMartNormalSubTotal { get; set; }
        public int SpeedyMartExpressItemCount { get; set; }
        public int SpeedyMartNormalItemCount { get; set; }

        // Fee info for checkout UI
        public decimal DeliveryFee { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal FreeDeliveryThreshold { get; set; }

        // Platform-specific charge details (for HotBox)
        public decimal HotBoxPlatformCharge { get; set; }
        public decimal HotBoxCutleryCharge { get; set; }
        public decimal HotBoxGiftWrapCharge { get; set; }
        public decimal HotBoxLowCartFee { get; set; }

        // Platform-specific charge details (for SpeedyMart Normal)
        public decimal SpeedyMartNormalPlatformCharge { get; set; }
        public decimal SpeedyMartNormalCutleryCharge { get; set; }
        public decimal SpeedyMartNormalGiftWrapCharge { get; set; }
        public decimal SpeedyMartNormalLowCartFee { get; set; }

        // Platform-specific charge details (for SpeedyMart Express)
        public decimal SpeedyMartExpressPlatformCharge { get; set; }
        public decimal SpeedyMartExpressCutleryCharge { get; set; }
        public decimal SpeedyMartExpressGiftWrapCharge { get; set; }
        public decimal SpeedyMartExpressLowCartFee { get; set; }

        // Applied charges breakdown (for display in cart/order summary)
        public decimal AppliedPlatformCharge { get; set; }
        public decimal AppliedCutleryCharge { get; set; }
        public decimal AppliedGiftWrapCharge { get; set; }
        public decimal AppliedLowCartFee { get; set; }
    }
}
