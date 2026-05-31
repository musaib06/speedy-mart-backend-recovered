using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("product_attribute_dimensions")]
    public class ProductAttributeDimensionDM : SiffrumDomainModelBase<long>
    {
        [ForeignKey(nameof(Product))]
        [Required]
        [Column("product_id")]
        public long ProductId { get; set; }
        public virtual ProductDM? Product { get; set; }

        [Required]
        [Column("dimension_key")]
        [MaxLength(100)]
        public string DimensionKey { get; set; } = string.Empty;

        [Required]
        [Column("dimension_label")]
        [MaxLength(100)]
        public string DimensionLabel { get; set; } = string.Empty;

        [Column("display_type")]
        [MaxLength(20)]
        public string DisplayType { get; set; } = "button"; // button, swatch, text

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        // Optional list of allowed values for this dimension (JSON array of objects)
        [Column("values_json")]
        public string? ValuesJson { get; set; }
    }
}
