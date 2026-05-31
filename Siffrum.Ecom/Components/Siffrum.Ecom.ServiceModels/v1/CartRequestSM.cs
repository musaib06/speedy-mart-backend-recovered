using Siffrum.Ecom.ServiceModels.Enums;
using System.Collections.Generic;

namespace Siffrum.Ecom.ServiceModels.v1
{
    public class CartRequestSM
    {
        public long ProductVariantId { get; set; }
        public int Quantity { get; set; }

        public PlatformTypeSM PlatformType { get; set; }

        // SpeedyMart only: must be Normal(1) or Express(2).
        // For products with DeliverySpeedType=Both(3), client must explicitly choose one.
        // For HotBox items leave as null/0 (ignored).
        public DeliverySpeedTypeSM? DeliverySpeedType { get; set; }

        public List<SelectedToppingItem>? SelectedToppings { get; set; }
        public List<SelectedAddonItem>? SelectedAddons { get; set; }
    }

    public class SelectedToppingItem
    {
        public long ToppingId { get; set; }
        public string? ToppingName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class SelectedAddonItem
    {
        public long AddonProductId { get; set; }
        public string? AddonName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? NetworkImage { get; set; }
        public decimal? Stock { get; set; }
    }
}
