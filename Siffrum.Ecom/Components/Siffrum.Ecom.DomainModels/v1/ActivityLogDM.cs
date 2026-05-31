using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("activity_logs")]
    [Index(nameof(UserId))]
    [Index(nameof(UserType))]
    [Index(nameof(ActionType))]
    [Index(nameof(CreatedAt))]
    [Index(nameof(UserId), nameof(CreatedAt))]
    public class ActivityLogDM : SiffrumDomainModelBase<long>
    {
        [Column("user_id")]
        public long UserId { get; set; }

        [Column("user_type")]
        [MaxLength(20)]
        public string UserType { get; set; } = string.Empty; // Admin, Seller, SuperAdmin, etc.

        [Column("user_name")]
        [MaxLength(200)]
        public string UserName { get; set; } = string.Empty;

        [Column("user_email")]
        [MaxLength(200)]
        public string? UserEmail { get; set; }

        [Column("action_type")]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // Create, Update, Delete, Critical, etc.

        [Column("action_category")]
        [MaxLength(50)]
        public string ActionCategory { get; set; } = string.Empty; // Product, Order, Seller, User, etc.

        [Column("entity_type")]
        [MaxLength(50)]
        public string? EntityType { get; set; } // Product, Order, Seller, etc.

        [Column("entity_id")]
        public long? EntityId { get; set; }

        [Column("entity_name")]
        [MaxLength(200)]
        public string? EntityName { get; set; }

        [Column("description")]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Column("old_values")]
        public string? OldValues { get; set; } // JSON of old values before change

        [Column("new_values")]
        public string? NewValues { get; set; } // JSON of new values after change

        [Column("ip_address")]
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [Column("mac_address")]
        [MaxLength(50)]
        public string? MacAddress { get; set; }

        [Column("user_agent")]
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [Column("device_info")]
        [MaxLength(200)]
        public string? DeviceInfo { get; set; }

        [Column("platform")]
        [MaxLength(20)]
        public string? Platform { get; set; } // Web, Mobile, etc.

        [Column("success")]
        public bool Success { get; set; } = true;

        [Column("error_message")]
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }
    }
}
