namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard
{
    public class PerformanceMetricsSM
    {
        public double PerfectOrderRate { get; set; }
        public double FillRate { get; set; }
        public double DeliveredAccuracyRate { get; set; }
        public int TotalOrders { get; set; }
        public int PerfectOrders { get; set; }
        public int OnTimeDeliveries { get; set; }
        public int CompleteDeliveries { get; set; }
    }

    public class ReturnRateAnalyticsSM
    {
        public double OverallReturnRate { get; set; }
        public double RefundRate { get; set; }
        public double CancellationRate { get; set; }
        public int TotalOrders { get; set; }
        public int ReturnedOrders { get; set; }
        public int RefundedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public List<ReturnReasonSM> ReturnReasons { get; set; } = new();
    }

    public class ReturnReasonSM
    {
        public string Reason { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class CustomerLtvSM
    {
        public double AverageLtv { get; set; }
        public double TotalLtv { get; set; }
        public int TotalCustomers { get; set; }
        public List<CustomerLtvSegmentSM> Segments { get; set; } = new();
    }

    public class CustomerLtvSegmentSM
    {
        public string Segment { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public double AverageLtv { get; set; }
    }

    public class NpsMetricsSM
    {
        public double NpsScore { get; set; }
        public int Promoters { get; set; }
        public int Passives { get; set; }
        public int Detractors { get; set; }
        public int TotalResponses { get; set; }
        public double ResponseRate { get; set; }
        public double AverageRating { get; set; }
    }
}
