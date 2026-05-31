using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Siffrum.Ecom.DomainModels.Foundation.Base;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("in_app_notifications")]
    [Index(nameof(RecipientType), nameof(RecipientId))]
    [Index(nameof(RecipientId), nameof(IsRead))]
    public class InAppNotificationDM : DomainModelRootBase
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column("recipient_type")]
        public int RecipientType { get; set; }

        [Column("recipient_id")]
        public long? RecipientId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Column("reference_id")]
        public string? ReferenceId { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }
    }
}
