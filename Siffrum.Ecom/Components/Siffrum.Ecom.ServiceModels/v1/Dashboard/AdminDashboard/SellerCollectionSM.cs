namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard
{
    public class SellerCollectionSM
    {
        public long SellerId { get; set; }
        public string StoreName { get; set; } = "";
        public string SellerName { get; set; } = "";
        public decimal TotalCollection { get; set; }
        public decimal YesterdayCollection { get; set; }
        public decimal FilteredCollection { get; set; }
        public decimal MonthCollection { get; set; }
        public int TotalOrders { get; set; }
        public int FilteredOrders { get; set; }
    }
}
