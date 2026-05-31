using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.v1;
using System.Text.Json;

namespace Siffrum.Ecom.BAL.Base
{
    public class ActivityLogProcess : SiffrumBalBase
    {
        public ActivityLogProcess(IMapper mapper, ApiDbContext apiDbContext)
            : base(mapper, apiDbContext)
        {
        }

        /// <summary>
        /// Creates a new activity log entry
        /// </summary>
        public async Task<ActivityLogSM> CreateLogAsync(CreateActivityLogRequestSM request, string? ipAddress = null, string? macAddress = null, string? userAgent = null)
        {
            var log = new ActivityLogDM
            {
                UserId = request.UserId,
                UserType = request.UserType,
                UserName = request.UserName,
                UserEmail = request.UserEmail,
                ActionType = request.ActionType,
                ActionCategory = request.ActionCategory,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                EntityName = request.EntityName,
                Description = request.Description,
                OldValues = request.OldValues != null ? JsonSerializer.Serialize(request.OldValues) : null,
                NewValues = request.NewValues != null ? JsonSerializer.Serialize(request.NewValues) : null,
                IpAddress = ipAddress,
                MacAddress = macAddress,
                UserAgent = userAgent,
                DeviceInfo = GetDeviceInfo(userAgent),
                Platform = "Web",
                Success = request.Success,
                ErrorMessage = request.ErrorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _apiDbContext.ActivityLogs.Add(log);
            await _apiDbContext.SaveChangesAsync();

            return MapToSM(log);
        }

        /// <summary>
        /// Gets activity logs with filtering and pagination
        /// </summary>
        public async Task<GetActivityLogsResponseSM> GetLogsAsync(GetActivityLogsRequestSM request)
        {
            var query = _apiDbContext.ActivityLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(request.UserType))
                query = query.Where(l => l.UserType == request.UserType);

            if (!string.IsNullOrEmpty(request.ActionType))
                query = query.Where(l => l.ActionType == request.ActionType);

            if (!string.IsNullOrEmpty(request.ActionCategory))
                query = query.Where(l => l.ActionCategory == request.ActionCategory);

            if (request.UserId.HasValue)
                query = query.Where(l => l.UserId == request.UserId.Value);

            if (request.FromDate.HasValue)
                query = query.Where(l => l.CreatedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(l => l.CreatedAt <= request.ToDate.Value);

            // Get total count
            var totalCount = await query.CountAsync();

            // Get paginated results
            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip(request.Skip)
                .Take(request.Take)
                .ToListAsync();

            return new GetActivityLogsResponseSM
            {
                Logs = logs.Select(MapToSM).ToList(),
                TotalCount = totalCount,
                PageSize = request.Take,
                CurrentPage = (request.Skip / request.Take) + 1
            };
        }

        /// <summary>
        /// Gets summary of activity logs for dashboard
        /// </summary>
        public async Task<ActivityLogSummarySM> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var todayEnd = today.AddDays(1);

            var totalLogs = await _apiDbContext.ActivityLogs.CountAsync();
            var todayLogs = await _apiDbContext.ActivityLogs
                .Where(l => l.CreatedAt >= today && l.CreatedAt < todayEnd)
                .CountAsync();
            var deleteActions = await _apiDbContext.ActivityLogs
                .Where(l => l.ActionType == "Delete")
                .CountAsync();
            var criticalActions = await _apiDbContext.ActivityLogs
                .Where(l => l.ActionType == "Critical")
                .CountAsync();
            var failedActions = await _apiDbContext.ActivityLogs
                .Where(l => !l.Success)
                .CountAsync();

            var recentActivities = await _apiDbContext.ActivityLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .Select(l => new RecentActivitySM
                {
                    Id = l.Id,
                    UserName = l.UserName,
                    UserType = l.UserType,
                    ActionType = l.ActionType,
                    ActionCategory = l.ActionCategory,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            return new ActivityLogSummarySM
            {
                TotalLogs = totalLogs,
                TodayLogs = todayLogs,
                DeleteActions = deleteActions,
                CriticalActions = criticalActions,
                FailedActions = failedActions,
                RecentActivities = recentActivities
            };
        }

        /// <summary>
        /// Gets distinct user types for filtering
        /// </summary>
        public async Task<List<string>> GetUserTypesAsync()
        {
            return await _apiDbContext.ActivityLogs
                .Select(l => l.UserType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        /// <summary>
        /// Gets distinct action types for filtering
        /// </summary>
        public async Task<List<string>> GetActionTypesAsync()
        {
            return await _apiDbContext.ActivityLogs
                .Select(l => l.ActionType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        /// <summary>
        /// Gets distinct action categories for filtering
        /// </summary>
        public async Task<List<string>> GetActionCategoriesAsync()
        {
            return await _apiDbContext.ActivityLogs
                .Select(l => l.ActionCategory)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
        }

        /// <summary>
        /// Gets logs for a specific entity
        /// </summary>
        public async Task<List<ActivityLogSM>> GetEntityLogsAsync(string entityType, long entityId, int take = 50)
        {
            var logs = await _apiDbContext.ActivityLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .ToListAsync();

            return logs.Select(MapToSM).ToList();
        }

        /// <summary>
        /// Gets logs for a specific user
        /// </summary>
        public async Task<List<ActivityLogSM>> GetUserLogsAsync(long userId, string? userType = null, int take = 100)
        {
            var query = _apiDbContext.ActivityLogs
                .Where(l => l.UserId == userId);

            if (!string.IsNullOrEmpty(userType))
                query = query.Where(l => l.UserType == userType);

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(take)
                .ToListAsync();

            return logs.Select(MapToSM).ToList();
        }

        private ActivityLogSM MapToSM(ActivityLogDM log)
        {
            return new ActivityLogSM
            {
                Id = log.Id,
                UserId = log.UserId,
                UserType = log.UserType,
                UserName = log.UserName,
                UserEmail = log.UserEmail,
                ActionType = log.ActionType,
                ActionCategory = log.ActionCategory,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                EntityName = log.EntityName,
                Description = log.Description,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                IpAddress = log.IpAddress,
                MacAddress = log.MacAddress,
                UserAgent = log.UserAgent,
                DeviceInfo = log.DeviceInfo,
                Platform = log.Platform,
                Success = log.Success,
                ErrorMessage = log.ErrorMessage,
                CreatedAt = log.CreatedAt ?? DateTime.UtcNow
            };
        }

        private string? GetDeviceInfo(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return null;

            if (userAgent.Contains("Mobile"))
                return "Mobile";
            if (userAgent.Contains("Tablet"))
                return "Tablet";
            if (userAgent.Contains("Windows"))
                return "Windows PC";
            if (userAgent.Contains("Mac"))
                return "Mac";
            if (userAgent.Contains("Linux"))
                return "Linux PC";

            return "Unknown";
        }
    }
}
