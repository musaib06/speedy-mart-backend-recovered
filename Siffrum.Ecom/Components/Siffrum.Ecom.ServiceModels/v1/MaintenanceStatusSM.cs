namespace Siffrum.Ecom.ServiceModels.v1
{
    public class MaintenanceStatusSM
    {
        public bool IsUnderMaintenance { get; set; }
        public DateTime? MaintenanceStartUtc { get; set; }
        public DateTime? MaintenanceEndUtc { get; set; }
        public string Message { get; set; }
    }
}
