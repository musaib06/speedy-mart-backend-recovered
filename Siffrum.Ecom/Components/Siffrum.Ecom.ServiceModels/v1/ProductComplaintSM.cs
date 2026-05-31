using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class ProductComplaintSM : SiffrumServiceModelBase<long>
    {
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public long SellerId { get; set; }
        public string? SellerName { get; set; }
        public int ComplaintType { get; set; }
        public string ComplaintTypeLabel => ComplaintType switch
        {
            1 => "Wrong Rejection",
            2 => "Category Issue",
            3 => "Price Issue",
            4 => "Other",
            _ => "Unknown"
        };
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string>? Attachments { get; set; }
        public int Status { get; set; } = 1;
        public string StatusLabel => Status switch
        {
            1 => "Open",
            2 => "In Progress",
            3 => "Resolved",
            4 => "Closed",
            _ => "Unknown"
        };
        public int Priority { get; set; } = 2;
        public string PriorityLabel => Priority switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            4 => "Urgent",
            _ => "Unknown"
        };
        public long? AssignedToAdminId { get; set; }
        public string? ResolutionNotes { get; set; }
        public long? ResolvedByAdminId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<ProductComplaintCommentSM>? Comments { get; set; }
    }

    public class ProductComplaintCommentSM : SiffrumServiceModelBase<long>
    {
        public long ComplaintId { get; set; }
        public int CommenterType { get; set; } // 1=Seller, 2=Admin
        public string CommenterTypeLabel => CommenterType == 1 ? "Seller" : "Admin";
        public long CommenterId { get; set; }
        public string? CommenterName { get; set; }
        public string Comment { get; set; } = string.Empty;
        public List<string>? Attachments { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
