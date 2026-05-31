using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.Foundation.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siffrum.Ecom.DomainModels.v1
{
    [Table("banners")]
    public class BannerDM : SiffrumDomainModelBase<long>
    {

        [Required]
        [Column("type")]
        public ExtensionTypeDM Extension { get; set; }

        [Required]
        [Column("banner_type")]
        public BannerTypeDM BannerType { get; set; }
        
        [Column("platform_type")]
        public PlatformTypeDM PlatformType { get; set; }
        public string Title { get; set; }

        [Column("sub_title")]
        public string SubTitle { get; set; }
        [Required]
        [Column("image")]
        public string ContentPath { get; set; } 

        [Column("slider_url")]
        public string? SliderUrl { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; }
        [Column("priority")]
        public int Priority { get; set; }

        // Delivery Speed targeting for SpeedyMart (0=N/A, 1=Normal, 2=Express)
        [Column("is_normal")]
        public bool IsNormal { get; set; } = true;  // Default: show for Normal delivery
        [Column("is_express")]
        public bool IsExpress { get; set; } = false; // Default: don't show for Express

        public ICollection<ProductBannerDM> BannerProducts { get; set; }
    }
}
