namespace Siffrum.Ecom.ServiceModels.v1
{
    public class WishlistItemSM
    {
        public long Id { get; set; }
        public long ProductVariantId { get; set; }
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }
        public string? ImageBase64 { get; set; }
        public string? NetworkImage { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public bool IsInStock { get; set; }
        public decimal Stock { get; set; }
        public string? UnitLabel { get; set; }
        public int DeliverySpeedType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
