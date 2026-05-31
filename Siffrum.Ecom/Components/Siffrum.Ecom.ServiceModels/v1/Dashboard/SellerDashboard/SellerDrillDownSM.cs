namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.SellerDashboard
{
    public class SellerOrderDrillDownItemSM
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string OrderStatus { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
    }

    public class LowStockPlatformSummarySM
    {
        public string Platform { get; set; } = "";
        public int Count { get; set; }
    }

    public class LowStockVariantSM
    {
        public long VariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string VariantName { get; set; } = "";
        public string? Image { get; set; }
        public double Stock { get; set; }
        public decimal Price { get; set; }
        public string? CategoryName { get; set; }
        public long? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? PlatformType { get; set; }
    }
}
