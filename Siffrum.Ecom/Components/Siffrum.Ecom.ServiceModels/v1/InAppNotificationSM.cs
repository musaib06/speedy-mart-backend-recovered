namespace Siffrum.Ecom.ServiceModels.v1
{
    public class InAppNotificationSM
    {
        public long Id { get; set; }
        public int RecipientType { get; set; }
        public long? RecipientId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class UnreadCountSM
    {
        public int Count { get; set; }
    }
}
