namespace Siffrum.Ecom.ServiceModels.v1
{
    /// <summary>
    /// Base delivery settings with radius constraints (for HotBox and SpeedyMart Express)
    /// </summary>
    public class DeliverySettingsConfig
    {
        public bool IsOrderPossible { get; set; } = true;
        public bool IsFreeDelivery { get; set; } = false;
        public bool IsCodAvailable { get; set; } = true;
        public int MinRadiusInKms { get; set; } = 0;
        public int MaxRadiusInKms { get; set; } = 100;
        public decimal MinDeliveryCharge { get; set; } = 0;
        public decimal DeliveryChargeAfterMinRadius { get; set; } = 0;
        public decimal CommissionPerKm { get; set; } = 0;

        // Surge pricing for this platform
        public bool IsSurge { get; set; } = false;
        public int SurgeCount { get; set; } = 0;
        public decimal SurgeCharge { get; set; } = 0;
    }

    /// <summary>
    /// Pan India delivery settings (for SpeedyMart Normal - no radius restrictions)
    /// </summary>
    public class PanIndiaDeliverySettingsConfig
    {
        public bool IsOrderPossible { get; set; } = true;
        public bool IsFreeDelivery { get; set; } = false;
        public bool IsCodAvailable { get; set; } = true;
        // NOTE: No MinRadius, MaxRadius, DeliveryChargeAfterMinRadius for Pan India
        public decimal MinDeliveryCharge { get; set; } = 0;
        public decimal CommissionPerKm { get; set; } = 0;

        // Surge pricing for this platform
        public bool IsSurge { get; set; } = false;
        public int SurgeCount { get; set; } = 0;
        public decimal SurgeCharge { get; set; } = 0;
    }

    public class SellerSettingsJson
    {
        // Legacy single settings (for backward compatibility)
        public bool IsOrderPossible { get; set; }
        public bool IsFreeDelivery { get; set; }
        public bool IsCodAvailable { get; set; }
        public int MinRadiusInKms { get; set; }
        public int MaxRadiusInKms { get; set; }

        // Legacy surge fields (for backward compatibility)
        public bool IsSurge { get; set; }
        public int SurgeCount { get; set; } = 0;
        public decimal SurgeCharge { get; set; }

        // Platform-specific surge pricing
        public SurgePricingConfig? SurgePricing { get; set; }

        public decimal MinDeliveryCharge { get; set; }
        public decimal DeliveryChargeAterMinRadius { get; set; }

        public decimal CommissionPerKm { get; set; }

        // Platform-specific delivery settings
        public DeliverySettingsConfig? HotBoxSettings { get; set; }
        public PanIndiaDeliverySettingsConfig? SpeedyMartNormalSettings { get; set; }
        public DeliverySettingsConfig? SpeedyMartExpressSettings { get; set; }
    }

    public class SurgePricingConfig
    {
        // Enable surge pricing per platform
        public bool EnableForHotBox { get; set; } = false;
        public bool EnableForSpeedyMartNormal { get; set; } = false;
        public bool EnableForSpeedyMartExpress { get; set; } = false;

        // Surge charges per platform
        public decimal HotBoxSurgeCharge { get; set; } = 0;
        public decimal SpeedyMartNormalSurgeCharge { get; set; } = 0;
        public decimal SpeedyMartExpressSurgeCharge { get; set; } = 0;

        // Threshold count for each platform
        public int HotBoxSurgeThreshold { get; set; } = 100;
        public int SpeedyMartNormalSurgeThreshold { get; set; } = 100;
        public int SpeedyMartExpressSurgeThreshold { get; set; } = 100;
    }
}