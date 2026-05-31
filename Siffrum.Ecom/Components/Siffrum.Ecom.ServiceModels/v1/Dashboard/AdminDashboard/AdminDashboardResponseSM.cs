namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard
{
    public class AdminDashboardResponseSM
    {
        public PlatformKpiSM PlatformKpis { get; set; } = new();
        public RevenueAnalyticsSM RevenueAnalytics { get; set; } = new();
        public VendorHealthSM VendorHealth { get; set; } = new();
        public OrderHealthSM OrderHealth { get; set; } = new();
        public List<PaymentModeCountSM> PaymentModeDistribution { get; set; } = new();
        public List<HourlyOrderSM> HourlyOrderDistribution { get; set; } = new();
        public List<CategoryRevenueSM> CategoryRevenue { get; set; } = new();
        public PerformanceMetricsSM PerformanceMetrics { get; set; } = new();
        public ReturnRateAnalyticsSM ReturnRateAnalytics { get; set; } = new();
        public CustomerLtvSM CustomerLtv { get; set; } = new();
        public NpsMetricsSM NpsMetrics { get; set; } = new();
        public GrowthMetricsSM GrowthMetrics { get; set; } = new();
        public HeatmapSM Heatmap { get; set; } = new();
    }

    public class PaymentModeCountSM
    {
        public string Mode { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class HourlyOrderSM
    {
        public int Hour { get; set; }
        public int Count { get; set; }
    }

    public class CategoryRevenueSM
    {
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }
}
