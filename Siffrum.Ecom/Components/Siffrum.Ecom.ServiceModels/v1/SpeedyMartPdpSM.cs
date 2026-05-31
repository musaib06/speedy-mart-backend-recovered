using System.Collections.Generic;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class SpeedyMartPdpVariantSM
    {
        public ProductVariantSM Variant { get; set; }
        public List<ProductImagesSM> Images { get; set; }
        public List<ProductSpecificationSM> Specifications { get; set; }
    }

    public class PdpRatingSummarySM
    {
        public decimal Rate { get; set; }
        public int TotalRatings { get; set; }
        public int RecommendPercent { get; set; }
        public List<PdpRatingTierSM> Tiers { get; set; } = new();
    }

    public class PdpRatingTierSM
    {
        public int Stars { get; set; }
        public int Count { get; set; }
    }

    public class PdpReviewSM
    {
        public string? UserName { get; set; }
        public short Rating { get; set; }
        public string? Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool VerifiedPurchase { get; set; }
    }

    public class PdpQaItemSM
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SpeedyMartPdpSM
    {
        public ProductSM Product { get; set; }
        public List<string> OverviewPoints { get; set; }
        public List<ProductAttributeDimensionSM> AttributeDimensions { get; set; }
        public List<SpeedyMartPdpVariantSM> Variants { get; set; }
        public PdpRatingSummarySM? Rating { get; set; }
        public List<PdpReviewSM> Reviews { get; set; } = new();
        public List<PdpQaItemSM> QaItems { get; set; } = new();
    }
}
