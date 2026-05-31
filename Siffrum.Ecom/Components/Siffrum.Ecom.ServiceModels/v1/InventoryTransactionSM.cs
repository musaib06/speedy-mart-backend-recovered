using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class InventoryTransactionSM : SiffrumServiceModelBase<long>
    {
        public long ProductVariantId { get; set; }
        public long? SellerId { get; set; }
        public string? SellerName { get; set; }
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }
        public string? PlatformType { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public decimal? QuantityBefore { get; set; }
        public decimal? QuantityAfter { get; set; }
        public decimal Delta { get; set; }
        public long? ReferenceId { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
