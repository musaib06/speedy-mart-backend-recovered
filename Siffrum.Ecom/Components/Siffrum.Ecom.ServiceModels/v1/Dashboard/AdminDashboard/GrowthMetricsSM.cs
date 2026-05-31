namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard
{
    public class GrowthMetricsSM
    {
        public DailyActiveUsersSM DailyActiveUsers { get; set; } = new();
        public MonthlyActiveUsersSM MonthlyActiveUsers { get; set; } = new();
        public UserRetentionSM UserRetention { get; set; } = new();
        public UserGrowthSM UserGrowth { get; set; } = new();
    }

    public class DailyActiveUsersSM
    {
        public int Today { get; set; }
        public int Yesterday { get; set; }
        public int WeekAverage { get; set; }
        public int MonthAverage { get; set; }
        public double ChangePercent { get; set; }
        public List<DauDataPointSM> Trend { get; set; } = new();
    }

    public class DauDataPointSM
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
        public int UniqueLogins { get; set; }
        public int UniqueOrders { get; set; }
        public int AppOpens { get; set; }
    }

    public class MonthlyActiveUsersSM
    {
        public int CurrentMonth { get; set; }
        public int PreviousMonth { get; set; }
        public int QuarterAverage { get; set; }
        public double ChangePercent { get; set; }
        public List<MauDataPointSM> Trend { get; set; } = new();
    }

    public class MauDataPointSM
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
        public int NewUsers { get; set; }
        public int ReturningUsers { get; set; }
    }

    public class UserRetentionSM
    {
        public double Day1Retention { get; set; } // Percentage
        public double Day7Retention { get; set; }
        public double Day30Retention { get; set; }
        public List<RetentionCohortSM> Cohorts { get; set; } = new();
    }

    public class RetentionCohortSM
    {
        public string CohortDate { get; set; } = string.Empty;
        public int InitialUsers { get; set; }
        public List<double> RetentionByDay { get; set; } = new(); // Day 0, 1, 7, 30 retention %
    }

    public class UserGrowthSM
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int NewUsersThisWeek { get; set; }
        public int NewUsersThisMonth { get; set; }
        public double GrowthRate { get; set; } // Percentage
        public List<GrowthDataPointSM> DailySignups { get; set; } = new();
    }

    public class GrowthDataPointSM
    {
        public string Date { get; set; } = string.Empty;
        public int NewUsers { get; set; }
        public int ChurnedUsers { get; set; }
        public int NetGrowth { get; set; }
    }
}
