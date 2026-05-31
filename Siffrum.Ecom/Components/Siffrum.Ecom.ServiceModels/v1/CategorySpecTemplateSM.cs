using Siffrum.Ecom.ServiceModels.Foundation.Base;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class CategorySpecTemplateSM : SiffrumServiceModelBase<long>
    {
        public long CategoryId { get; set; }
        public string SpecKey { get; set; } = string.Empty;
        public string SpecLabel { get; set; } = string.Empty;
        public string? SpecGroup { get; set; }
        public string? Placeholder { get; set; }
        public bool IsRequired { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }
}
