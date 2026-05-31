using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class CategoryAttrDimensionSM : SiffrumServiceModelBase<long>
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ValuesJson { get; set; }
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
    }
}
