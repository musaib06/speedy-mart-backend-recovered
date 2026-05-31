using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("category_spec_templates")]
    public class CategorySpecTemplateDM : SiffrumDomainModelBase<long>
    {
        [ForeignKey(nameof(Category))]
        [Required]
        [Column("category_id")]
        public long CategoryId { get; set; }
        public virtual CategoryDM? Category { get; set; }

        [Required]
        [Column("spec_key")]
        [MaxLength(100)]
        public string SpecKey { get; set; } = string.Empty;

        [Required]
        [Column("spec_label")]
        [MaxLength(100)]
        public string SpecLabel { get; set; } = string.Empty;

        [Column("spec_group")]
        [MaxLength(50)]
        public string? SpecGroup { get; set; }  // General, Performance, Camera, etc.

        [Column("placeholder")]
        [MaxLength(200)]
        public string? Placeholder { get; set; }  // Input hint e.g. "e.g. 5088 mAh"

        [Column("is_required")]
        public bool IsRequired { get; set; } = false;

        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }
    }
}
