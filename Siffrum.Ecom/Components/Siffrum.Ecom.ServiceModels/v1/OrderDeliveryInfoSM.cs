namespace Siffrum.Ecom.ServiceModels.v1
{
    public class OrderDeliveryInfoSM
    {
        public long? DeliveryBoyId { get; set; }
        public string? DeliveryBoyName { get; set; }
        public string? DeliveryBoyMobile { get; set; }
        public string? DeliveryStatus { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
