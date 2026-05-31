using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("product_specifications")]
    public class ProductSpecificationDM : SiffrumDomainModelBase<long>
    {

        [Column("key")]
        public string Key { get; set; }

        [Column("value")]
        public string Value { get; set; }

        // SpeedyMart-specific fields
        [Column("specification_group")]
        [MaxLength(50)]
        public string? SpecificationGroup { get; set; }  // General, Performance, etc.

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [Column("is_filterable")]
        public bool IsFilterable { get; set; } = false;

        [ForeignKey(nameof(ProductVariant))]
        [Required]
        [Column("product_variant_id")]
        public long ProductVariantId { get; set; }
        public virtual ProductVariantDM? ProductVariant { get; set; }
       

    }
}
