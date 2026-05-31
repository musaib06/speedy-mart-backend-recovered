namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard
{
    public class SellerOrderSummarySM
    {
        public long SellerId { get; set; }
        public string StoreName { get; set; } = "";
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class OrderDrillDownItemSM
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string OrderStatus { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string PaymentMode { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public string PlatformType { get; set; } = "";
    }
}
