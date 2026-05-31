using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("category_attr_dimensions")]
    public class CategoryAttrDimensionDM : SiffrumDomainModelBase<long>
    {
        [ForeignKey(nameof(Category))]
        [Required]
        [Column("category_id")]
        public long CategoryId { get; set; }
        public virtual CategoryDM? Category { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;  // e.g. "Color", "Size", "RAM"

        [Column("values_json")]
        public string? ValuesJson { get; set; }  // JSON array: ["Red","Blue","Green"]

        [Column("is_required")]
        public bool IsRequired { get; set; } = false;

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;
    }
}
