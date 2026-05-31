namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard
{
    public class HeatmapSM
    {
        public List<HourlyActivitySM> HourlyActivity { get; set; } = new();
        public List<DayOfWeekActivitySM> DayOfWeekActivity { get; set; } = new();
        public List<GeographicActivitySM> GeographicActivity { get; set; } = new();
        public List<PageViewHeatmapSM> PageViews { get; set; } = new();
        public HeatmapSummarySM Summary { get; set; } = new();
    }

    public class HourlyActivitySM
    {
        public int Hour { get; set; } // 0-23
        public string HourLabel { get; set; } = string.Empty; // "12 AM", "1 PM", etc.
        public int UserCount { get; set; }
        public int OrderCount { get; set; }
        public int SessionCount { get; set; }
        public double Intensity { get; set; } // 0.0 to 1.0 for heatmap coloring
    }

    public class DayOfWeekActivitySM
    {
        public int DayOfWeek { get; set; } // 0=Sunday, 6=Saturday
        public string DayName { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int OrderCount { get; set; }
        public int SessionCount { get; set; }
        public double Intensity { get; set; }
    }

    public class GeographicActivitySM
    {
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Intensity { get; set; }
    }

    public class PageViewHeatmapSM
    {
        public string PagePath { get; set; } = string.Empty;
        public string PageName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int UniqueVisitors { get; set; }
        public double AvgTimeOnPage { get; set; } // in seconds
        public double BounceRate { get; set; } // percentage
        public double Intensity { get; set; }
    }

    public class HeatmapSummarySM
    {
        public int PeakHour { get; set; }
        public string PeakHourLabel { get; set; } = string.Empty;
        public int PeakDayOfWeek { get; set; }
        public string PeakDayLabel { get; set; } = string.Empty;
        public string TopCity { get; set; } = string.Empty;
        public string TopPage { get; set; } = string.Empty;
        public double AverageSessionDuration { get; set; } // in minutes
        public int TotalSessions { get; set; }
    }

    public class HeatmapRequestSM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Platform { get; set; } // "All", "HotBox", "SpeedyMart"
        public string? UserType { get; set; } // "All", "User", "Seller", "DeliveryBoy"
    }
}
