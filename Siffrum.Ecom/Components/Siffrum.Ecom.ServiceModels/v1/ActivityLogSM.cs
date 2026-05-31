namespace Siffrum.Ecom.ServiceModels.v1
{
    public class ActivityLogSM
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ActionCategory { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public long? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? DeviceInfo { get; set; }
        public string? Platform { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateActivityLogRequestSM
    {
        public long UserId { get; set; }
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string ActionType { get; set; } = string.Empty; // Create, Update, Delete, Critical, Login, Logout, etc.
        public string ActionCategory { get; set; } = string.Empty; // Product, Order, Seller, User, Category, etc.
        public string? EntityType { get; set; }
        public long? EntityId { get; set; }
        public string? EntityName { get; set; }
        public string Description { get; set; } = string.Empty;
        public object? OldValues { get; set; }
        public object? NewValues { get; set; }
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    public class GetActivityLogsRequestSM
    {
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
        public string? UserType { get; set; } // Admin, Seller, SuperAdmin
        public string? ActionType { get; set; } // Create, Update, Delete, Critical
        public string? ActionCategory { get; set; } // Product, Order, Seller
        public long? UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class GetActivityLogsResponseSM
    {
        public List<ActivityLogSM> Logs { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
    }

    public class ActivityLogSummarySM
    {
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int DeleteActions { get; set; }
        public int CriticalActions { get; set; }
        public int FailedActions { get; set; }
        public List<RecentActivitySM> RecentActivities { get; set; } = new();
    }

    public class RecentActivitySM
    {
        public long Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionCategory { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
