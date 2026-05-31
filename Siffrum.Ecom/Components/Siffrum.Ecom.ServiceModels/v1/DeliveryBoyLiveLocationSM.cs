namespace Siffrum.Ecom.ServiceModels.v1
{
    /// <summary>
    /// Live location and status of a delivery boy for admin tracking
    /// </summary>
    public class DeliveryBoyLiveLocationSM
    {
        // Delivery Boy Info
        public long DeliveryBoyId { get; set; }
        public string DeliveryBoyName { get; set; } = string.Empty;
        public string? DeliveryBoyMobile { get; set; }
        public string? DeliveryBoyImage { get; set; }

        // Current Delivery Info
        public long? DeliveryId { get; set; }
        public long? OrderId { get; set; }
        public string? OrderNumber { get; set; }
        public string? OrderStatus { get; set; }
        public decimal? OrderAmount { get; set; }

        // Customer Info
        public string? CustomerName { get; set; }
        public string? CustomerMobile { get; set; }

        // Seller Info
        public string? SellerName { get; set; }

        // Location
        public double CurrentLat { get; set; }
        public double CurrentLong { get; set; }
        public DateTime? LastUpdated { get; set; }

        // Online Status
        public bool IsOnline { get; set; }
    }
}
