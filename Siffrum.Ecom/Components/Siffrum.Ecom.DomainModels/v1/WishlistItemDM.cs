using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("wishlist_items")]
    public class WishlistItemDM
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [ForeignKey(nameof(User))]
        [Column("user_id")]
        public long UserId { get; set; }
        public virtual UserDM User { get; set; }

        [ForeignKey(nameof(ProductVariant))]
        [Column("product_variant_id")]
        public long ProductVariantId { get; set; }
        public virtual ProductVariantDM ProductVariant { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
