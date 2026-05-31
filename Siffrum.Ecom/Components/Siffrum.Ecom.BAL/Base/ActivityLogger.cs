using AutoMapper;
using Microsoft.AspNetCore.Http;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.v1;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Siffrum.Ecom.BAL.Base
{
    /// <summary>
    /// Helper class for logging activities with IP/MAC address tracking
    /// </summary>
    public class ActivityLogger
    {
        private readonly ActivityLogProcess _activityLogProcess;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogger(ActivityLogProcess activityLogProcess, IHttpContextAccessor httpContextAccessor)
        {
            _activityLogProcess = activityLogProcess;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Logs an activity with automatic IP/MAC address capture
        /// </summary>
        public async Task<ActivityLogSM> LogAsync(CreateActivityLogRequestSM request)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            string? ipAddress = null;
            string? macAddress = null;
            string? userAgent = null;

            if (httpContext != null)
            {
                // Get IP Address
                ipAddress = GetClientIpAddress(httpContext);

                // Get User Agent
                userAgent = httpContext.Request.Headers.UserAgent.FirstOrDefault();

                // Generate device fingerprint from IP + User Agent (since MAC is not accessible from browsers)
                var fingerprintInput = $"{ipAddress ?? "unknown"}|{userAgent ?? "unknown"}";
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fingerprintInput));
                    macAddress = "FP-" + Convert.ToHexString(hash).Substring(0, 16);
                }
            }

            return await _activityLogProcess.CreateLogAsync(request, ipAddress, macAddress, userAgent);
        }

        /// <summary>
        /// Logs a create action
        /// </summary>
        public async Task<ActivityLogSM> LogCreateAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail,
            string category,
            string entityType,
            long? entityId,
            string? entityName,
            string description,
            object? newValues = null)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = "Create",
                ActionCategory = category,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                NewValues = newValues,
                Success = true
            });
        }

        /// <summary>
        /// Logs an update action
        /// </summary>
        public async Task<ActivityLogSM> LogUpdateAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail,
            string category,
            string entityType,
            long? entityId,
            string? entityName,
            string description,
            object? oldValues = null,
            object? newValues = null)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = "Update",
                ActionCategory = category,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                OldValues = oldValues,
                NewValues = newValues,
                Success = true
            });
        }

        /// <summary>
        /// Logs a delete action
        /// </summary>
        public async Task<ActivityLogSM> LogDeleteAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail,
            string category,
            string entityType,
            long? entityId,
            string? entityName,
            string description,
            object? oldValues = null)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = "Delete",
                ActionCategory = category,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                OldValues = oldValues,
                Success = true
            });
        }

        /// <summary>
        /// Logs a critical action
        /// </summary>
        public async Task<ActivityLogSM> LogCriticalAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail,
            string category,
            string entityType,
            long? entityId,
            string? entityName,
            string description,
            object? oldValues = null,
            object? newValues = null)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = "Critical",
                ActionCategory = category,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                Description = description,
                OldValues = oldValues,
                NewValues = newValues,
                Success = true
            });
        }

        /// <summary>
        /// Logs a login action
        /// </summary>
        public async Task<ActivityLogSM> LogLoginAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail,
            bool success = true,
            string? errorMessage = null)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = "Login",
                ActionCategory = "Authentication",
                Description = success ? $"{userType} {userName} logged in successfully" : $"Failed login attempt for {userType} {userName}",
                Success = success,
                ErrorMessage = errorMessage
            });
        }

        /// <summary>
        /// Logs a logout action
        /// </summary>
        public async Task<ActivityLogSM> LogLogoutAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = "Logout",
                ActionCategory = "Authentication",
                Description = $"{userType} {userName} logged out",
                Success = true
            });
        }

        /// <summary>
        /// Logs a failed action
        /// </summary>
        public async Task<ActivityLogSM> LogFailedAsync(
            long userId,
            string userType,
            string userName,
            string? userEmail,
            string actionType,
            string category,
            string description,
            string errorMessage)
        {
            return await LogAsync(new CreateActivityLogRequestSM
            {
                UserId = userId,
                UserType = userType,
                UserName = userName,
                UserEmail = userEmail,
                ActionType = actionType,
                ActionCategory = category,
                Description = description,
                Success = false,
                ErrorMessage = errorMessage
            });
        }

        /// <summary>
        /// Gets the client IP address from the HTTP context
        /// </summary>
        private string? GetClientIpAddress(HttpContext httpContext)
        {
            // Check for forwarded headers (when behind proxy/load balancer)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                var ips = forwardedFor.Split(',');
                if (ips.Length > 0)
                    return ips[0].Trim();
            }

            // Check X-Real-IP header
            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
                return realIp;

            // Fall back to connection remote IP
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }
    }

    /// <summary>
    /// Extension methods for ControllerBase to easily log activities
    /// </summary>
    public static class ActivityLoggerExtensions
    {
        /// <summary>
        /// Gets user details from claims for logging
        /// </summary>
        public static (long UserId, string UserType, string UserName, string? UserEmail) GetUserDetailsForLogging(this Microsoft.AspNetCore.Mvc.ControllerBase controller)
        {
            var user = controller.User;

            var userId = user.FindFirst("dbRId")?.Value;
            var userType = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown";
            var userName = user.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
            var userEmail = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            return (long.TryParse(userId, out var id) ? id : 0, userType, userName, userEmail);
        }
    }
}
