namespace Siffrum.Ecom.ServiceModels.v1
{
    public class RiderEarningSummarySM
    {
        public long DeliveryBoyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public int TotalDeliveries { get; set; }
        public decimal TodayCommission { get; set; }
        public decimal MonthlyCommission { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal TotalTip { get; set; }
    }
}
