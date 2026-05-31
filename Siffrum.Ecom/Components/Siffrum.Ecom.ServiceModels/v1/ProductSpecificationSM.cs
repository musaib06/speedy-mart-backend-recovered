using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class ProductSpecificationSM : SiffrumServiceModelBase<long>
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public long ProductVariantId { get; set; }

        // SpeedyMart-specific fields
        public string? SpecificationGroup { get; set; }  // General, Performance, etc.
        public int DisplayOrder { get; set; } = 0;
        public bool IsFilterable { get; set; } = false;
    }
}
