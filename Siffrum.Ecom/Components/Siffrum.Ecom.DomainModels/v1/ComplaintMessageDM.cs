using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("complaint_messages")]
    public class ComplaintMessageDM : SiffrumDomainModelBase<long>
    {
        [Column("complaint_id")]
        public long ComplaintId { get; set; }

        [ForeignKey(nameof(ComplaintId))]
        public virtual OrderComplaintDM Complaint { get; set; }

        [Required]
        [Column("sender_type")]
        [MaxLength(20)]
        public string SenderType { get; set; } // "User" or "Seller"

        [Column("sender_id")]
        public long SenderId { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("image_url")]
        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }
}
