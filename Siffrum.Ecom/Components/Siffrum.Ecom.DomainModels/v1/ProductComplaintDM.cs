using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("product_complaints")]
    public class ProductComplaintDM : SiffrumDomainModelBase<long>
    {
        [Column("product_id")]
        public long ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public virtual ProductDM? Product { get; set; }

        [Column("seller_id")]
        public long SellerId { get; set; }

        [Column("complaint_type")]
        public int ComplaintType { get; set; } // 1=WrongRejection, 2=CategoryIssue, 3=PriceIssue, 4=Other

        [Required]
        [Column("subject")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("attachments")]
        public string? Attachments { get; set; } // JSON array

        [Column("status")]
        public int Status { get; set; } = 1; // 1=Open, 2=InProgress, 3=Resolved, 4=Closed

        [Column("priority")]
        public int Priority { get; set; } = 2; // 1=Low, 2=Medium, 3=High, 4=Urgent

        [Column("assigned_to_admin_id")]
        public long? AssignedToAdminId { get; set; }

        [Column("resolution_notes")]
        public string? ResolutionNotes { get; set; }

        [Column("resolved_by_admin_id")]
        public long? ResolvedByAdminId { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("platform_type")]
        public PlatformTypeDM PlatformType { get; set; } = PlatformTypeDM.SpeedyMart;

        public virtual ICollection<ProductComplaintCommentDM> Comments { get; set; } = new List<ProductComplaintCommentDM>();
    }
}
