using Siffrum.Ecom.ServiceModels.Enums;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class UserSpeedyMartProductSM
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string Name { get; set; }
        public string? UnitLabel { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public string ImageBase64 { get; set; }
        public string? NetworkImage { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountedPercentage { get; set; }
        public int? TotalAllowedQuantity { get; set; }
        public bool IsCodAllowed { get; set; }
        public decimal? Stock { get; set; } = 0.00m;
        public List<ProductTagSM> ProductTags { get; set; }

        public long? CategoryId { get; set; }
        public short Rate { get; set; }
        public int TotalRatings { get; set; }
        public bool IsFreshArrival { get; set; }
        public bool IsBestSeller { get; set; }

        // SpeedyMart delivery speed type: 1=Normal, 2=Express, 3=Both
        public DeliverySpeedTypeSM DeliverySpeedType { get; set; }

        // Display tags for the product (e.g., ["express"], ["normal"], or ["express", "normal"])
        public List<string> Tags { get; set; } = new List<string>();
    }
}
