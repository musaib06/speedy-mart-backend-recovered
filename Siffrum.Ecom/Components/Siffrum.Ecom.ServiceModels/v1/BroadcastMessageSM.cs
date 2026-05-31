namespace Siffrum.Ecom.ServiceModels.v1
{
    public class BroadcastMessageSM
    {
        public string Title { get; set; }
        public string Message { get; set; }

        /// <summary>
        /// "AllUsers", "SelectedUsers", "AllSellers", "SelectedSellers"
        /// </summary>
        public string TargetType { get; set; }

        /// <summary>
        /// Required when TargetType is "SelectedUsers" or "SelectedSellers"
        /// </summary>
        public List<long>? RecipientIds { get; set; }
    }

    public class BroadcastResultSM
    {
        public bool Success { get; set; }
        public int TotalRecipients { get; set; }
        public int PushSent { get; set; }
        public int InAppSent { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
