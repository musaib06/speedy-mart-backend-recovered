using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class LowStockAlertSM : SiffrumServiceModelBase<long>
    {
        public long ProductVariantId { get; set; }
        public string? VariantName { get; set; }
        public string? ProductName { get; set; }
        public long SellerId { get; set; }
        public string? SellerName { get; set; }
        public int ThresholdQuantity { get; set; } = 5;
        public bool IsActive { get; set; } = true;
        public DateTime? LastAlertSentAt { get; set; }
        public decimal? CurrentStock { get; set; }
    }
}
