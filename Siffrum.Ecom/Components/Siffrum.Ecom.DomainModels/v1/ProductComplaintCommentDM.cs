using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("product_complaint_comments")]
    public class ProductComplaintCommentDM : SiffrumDomainModelBase<long>
    {
        [Column("complaint_id")]
        public long ComplaintId { get; set; }

        [ForeignKey(nameof(ComplaintId))]
        public virtual ProductComplaintDM? Complaint { get; set; }

        [Column("commenter_type")]
        public int CommenterType { get; set; } // 1=Seller, 2=Admin

        [Column("commenter_id")]
        public long CommenterId { get; set; }

        [Required]
        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        [Column("attachments")]
        public string? Attachments { get; set; } // JSON array
    }
}
