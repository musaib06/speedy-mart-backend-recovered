using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class ProductAttributeDimensionSM : SiffrumServiceModelBase<long>
    {
        public long ProductId { get; set; }
        public string DimensionKey { get; set; } = string.Empty;
        public string DimensionLabel { get; set; } = string.Empty;
        public string DisplayType { get; set; } = "button";
        public int DisplayOrder { get; set; } = 0;
        public List<DimensionValueSM> Values { get; set; } = new();
    }

    public class DimensionValueSM
    {
        public string Value { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsSelected { get; set; }
    }
}
