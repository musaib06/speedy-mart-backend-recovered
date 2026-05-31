using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class ComplaintMessageSM : SiffrumServiceModelBase<long>
    {
        public long ComplaintId { get; set; }
        public string SenderType { get; set; } // "User" or "Seller"
        public long SenderId { get; set; }
        public string? Message { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class ComplaintChatInfoSM
    {
        public long ComplaintId { get; set; }
        public string Status { get; set; }
        public int UserMessageCount { get; set; }
        public int MaxUserMessages { get; set; } = 5;
        public int RemainingMessages { get; set; }
        public bool CanUserSend { get; set; }
        public bool HasImage { get; set; }
        public bool CanAttachImage { get; set; }
        public List<ComplaintMessageSM> Messages { get; set; } = new();
    }

    public class SendComplaintMessageSM
    {
        public string? Message { get; set; }
        public string? ImageBase64 { get; set; }
        public string? ImageExtension { get; set; }
    }
}
