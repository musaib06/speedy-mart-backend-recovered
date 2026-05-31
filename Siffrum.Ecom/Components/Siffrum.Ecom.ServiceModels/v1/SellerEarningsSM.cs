namespace Siffrum.Ecom.ServiceModels.v1
{
    public class SellerEarningsSM
    {
        public decimal TotalEarnings { get; set; }
        public decimal TodayEarnings { get; set; }
        public decimal YesterdayEarnings { get; set; }
        public decimal WeekEarnings { get; set; }
        public decimal MonthEarnings { get; set; }

        public int TotalOrders { get; set; }
        public int TodayOrders { get; set; }
        public int YesterdayOrders { get; set; }
        public int WeekOrders { get; set; }
        public int MonthOrders { get; set; }

        public decimal CommissionRate { get; set; }
        public decimal TotalCommission { get; set; }
    }
}
