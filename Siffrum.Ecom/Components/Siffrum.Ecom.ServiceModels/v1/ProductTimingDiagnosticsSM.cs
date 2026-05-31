namespace Siffrum.Ecom.ServiceModels.v1
{
    /// <summary>
    /// Diagnostics result for troubleshooting why a product isn't showing in HotBox
    /// </summary>
    public class ProductTimingDiagnosticsSM
    {
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public DateTime CheckedAt { get; set; }
        public string? Timezone { get; set; }
        public string? CurrentTime { get; set; }
        public int CurrentHour { get; set; }
        public int TimingCount { get; set; }
        public bool HasActiveVariants { get; set; }
        public bool IsCurrentlyAvailable { get; set; }
        public string? ExpectedCategoryTiming { get; set; }
        public string? ExpectedTimeRange { get; set; }

        public List<TimingCheckDetail> TimingChecks { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class TimingCheckDetail
    {
        public long TimingId { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool CrossesMidnight { get; set; }
        public bool IsCurrentlyActive { get; set; }
        public List<string> Issues { get; set; } = new();
    }
}
