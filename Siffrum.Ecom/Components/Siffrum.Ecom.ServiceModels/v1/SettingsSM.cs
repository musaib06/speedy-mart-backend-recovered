using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    /// <summary>
    /// Platform-specific charge configuration
    /// </summary>
    public class PlatformChargeConfig
    {
        public decimal PlatformCharge { get; set; } = 0;
        public decimal CutleryCharge { get; set; } = 0;
        public decimal GiftWrapCharge { get; set; } = 0;
        public decimal LowCartFeeCharge { get; set; } = 0;
        public decimal LowCartAmountValue { get; set; } = 0;
    }

    public class SettingsSM : SiffrumServiceModelBase<long>
    {
        public bool IsOrderPossible { get; set; }
        public bool IsFreeDelivery { get; set; }
        public bool IsSurge { get; set; }
        public int DeliveryInMinutes { get; set; }
        public int SurgeCount { get; set; } = 0;
        public decimal SurgeCharge { get; set; }
        public int ReferralPercentage { get; set; }

        // Legacy single values (kept for backward compatibility)
        public decimal PlatormCharge { get; set; }
        public decimal CutlaryCharge { get; set; }
        public decimal GiftWrapCharge { get; set; }
        public decimal LowCartFeeCharge { get; set; }
        public decimal LowCartAmountValue { get; set; }

        // NEW: Platform-specific charges
        public PlatformChargeConfig HotBoxCharges { get; set; } = new PlatformChargeConfig();
        public PlatformChargeConfig SpeedyMartNormalCharges { get; set; } = new PlatformChargeConfig();
        public PlatformChargeConfig SpeedyMartExpressCharges { get; set; } = new PlatformChargeConfig();

        public bool IsCodAvailable { get; set; }

        public decimal CommissionPerKm { get; set; }

        // Maintenance Mode
        public bool IsMaintenanceMode { get; set; }
        public DateTime? MaintenanceStartUtc { get; set; }
        public DateTime? MaintenanceEndUtc { get; set; }
        public string MaintenanceMessage { get; set; }

        // SpeedyMart Settings
        public bool IsSpeedyMartExpressEnabled { get; set; }
        public bool IsSpeedyMartNormalEnabled { get; set; }
        public int SpeedyMartExpressDeliveryMinMinutes { get; set; }
        public int SpeedyMartExpressDeliveryMaxMinutes { get; set; }
        public int SpeedyMartNormalDeliveryMinHours { get; set; }
        public int SpeedyMartNormalDeliveryMaxHours { get; set; }
        public decimal SpeedyMartExpressDeliveryCharge { get; set; }
        public decimal SpeedyMartNormalDeliveryCharge { get; set; }
        public decimal SpeedyMartExpressMinOrderAmount { get; set; }
        public decimal SpeedyMartNormalMinOrderAmount { get; set; }

    }
}
