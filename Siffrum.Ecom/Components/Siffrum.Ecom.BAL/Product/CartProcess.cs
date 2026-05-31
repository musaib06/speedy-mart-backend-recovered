using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.BAL.LoginUsers;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class CartProcess : SiffrumBalOdataBase<CartSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ProductVariantProcess _productVariantProcess;
        private readonly ImageProcess _imageProcess;
        private readonly SettingsProcess _settingsProcess;

        public CartProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ProductVariantProcess productVariantProcess,
            ILoginUserDetail loginUserDetail,
            ImageProcess imageProcess,
            SettingsProcess settingsProcess)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _productVariantProcess = productVariantProcess;
            _imageProcess = imageProcess;
            _settingsProcess = settingsProcess;
        }

        #region ODATA

        public override async Task<IQueryable<CartSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.Carts.AsNoTracking();
            return await base.MapEntityAsToQuerable<CartDM, CartSM>(_mapper, entitySet);
        }

        #endregion

        #region GET OR CREATE CART

        public async Task<CartDM> GetOrCreateCartInternal(long userId, PlatformTypeDM platform)
        {
            var cart = await _apiDbContext.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PlatformType == platform);

            if (cart != null)
                return cart;

            cart = new CartDM
            {
                UserId = userId,
                PlatformType = platform,
                SubTotal = 0,
                TaxAmount = 0,
                DiscountAmount = 0,
                GrandTotal = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId,
                Items = new List<CartItemDM>()
            };

            await _apiDbContext.Carts.AddAsync(cart);

            try
            {
                await _apiDbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                cart = await _apiDbContext.Carts
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.UserId == userId && x.PlatformType == platform);
            }

            return cart;
        }

        #endregion

        #region ADD TO CART

        public async Task<CombineCartSM> AddToCart(long userId, CartRequestSM req)
        {
            var productVariantId = req.ProductVariantId;
            var quantity = req.Quantity;
            var platformSM = req.PlatformType;

            if (quantity < 0)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Quantity cannot be negative");

            var platform = (PlatformTypeDM)platformSM;

            var product = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == productVariantId);

            if (product == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Product not found");

            if (product.PlatformType != platform)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Product does not belong to selected platform");

            // ── SpeedyMart: validate and resolve delivery speed ──────────────────────
            DeliverySpeedTypeDM? resolvedSpeed = null;
            if (platform == PlatformTypeDM.SpeedyMart)
            {
                var productSpeed = (DeliverySpeedTypeDM)product.DeliverySpeedType;
                var requestedSpeed = req.DeliverySpeedType.HasValue
                    ? (DeliverySpeedTypeDM)req.DeliverySpeedType.Value
                    : (DeliverySpeedTypeDM?)null;

                if (productSpeed == DeliverySpeedTypeDM.Normal)
                {
                    resolvedSpeed = DeliverySpeedTypeDM.Normal;
                }
                else if (productSpeed == DeliverySpeedTypeDM.Express)
                {
                    resolvedSpeed = DeliverySpeedTypeDM.Express;
                }
                else // Both
                {
                    if (requestedSpeed == null ||
                        (requestedSpeed != DeliverySpeedTypeDM.Normal && requestedSpeed != DeliverySpeedTypeDM.Express))
                        throw new SiffrumException(
                            ApiErrorTypeSM.InvalidInputData_NoLog,
                            "This product supports both Express and Normal delivery. Please specify deliverySpeedType=1 (Normal) or 2 (Express).");
                    resolvedSpeed = requestedSpeed;
                }
            }

            var cart = await GetOrCreateCartInternal(userId, platform);

            // Enforce Min/Max/Step rules (integer quantities)
            int NormalizeQuantity(ProductVariantDM pv, int reqQty)
            {
                int q = Math.Max(reqQty, 0);
                var min = pv.MinOrderQty.HasValue ? (int)Math.Ceiling((double)pv.MinOrderQty.Value) : 0;
                var max = pv.MaxOrderQty.HasValue ? (int)Math.Floor((double)pv.MaxOrderQty.Value) : int.MaxValue;
                var step = pv.OrderStepQty.HasValue && pv.OrderStepQty.Value > 0 ? (int)Math.Ceiling((double)pv.OrderStepQty.Value) : 1;
                if (q < min) q = min;
                if (step > 1 && q > 0)
                {
                    var rem = q % step;
                    if (rem != 0) q += (step - rem);
                }
                if (q > max)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, $"Maximum allowed quantity is {max}");
                return q;
            }

            // For SpeedyMart: key cart items by (variantId + deliverySpeed) so same
            // product can exist in both Express and Normal sub-carts simultaneously
            var existingItem = platform == PlatformTypeDM.SpeedyMart
                ? cart.Items.FirstOrDefault(x =>
                    x.ProductVariantId == productVariantId &&
                    x.DeliverySpeedType == resolvedSpeed)
                : cart.Items.FirstOrDefault(x => x.ProductVariantId == productVariantId);

            // ── Resolve topping & addon prices from DB (server is source of truth) ──
            decimal toppingsTotal = 0;
            decimal addonsTotal = 0;
            string? toppingsJson = null;
            string? addonsJson = null;

            if (req.SelectedToppings != null && req.SelectedToppings.Any())
            {
                var toppingIds = req.SelectedToppings.Select(t => t.ToppingId).ToList();
                var dbToppings = await _apiDbContext.ProductTopping
                    .AsNoTracking()
                    .Where(pt => pt.ProductId == product.ProductId && toppingIds.Contains(pt.ToppingId))
                    .ToDictionaryAsync(pt => pt.ToppingId, pt => pt.Price);

                foreach (var t in req.SelectedToppings)
                {
                    if (dbToppings.TryGetValue(t.ToppingId, out var dbPrice))
                        t.Price = dbPrice;
                    // else: keep client price as fallback (topping might not be linked yet)
                }
                toppingsTotal = req.SelectedToppings.Sum(t => t.Price * t.Quantity);
                toppingsJson = JsonConvert.SerializeObject(req.SelectedToppings);
            }
            if (req.SelectedAddons != null && req.SelectedAddons.Any())
            {
                var addonProductIds = req.SelectedAddons.Select(a => a.AddonProductId).ToList();
                var dbAddonVariants = await _apiDbContext.ProductVariant
                    .AsNoTracking()
                    .Include(pv => pv.Product)
                    .Where(pv => addonProductIds.Contains(pv.Id))
                    .ToListAsync();
                var dbAddonMap = dbAddonVariants.ToDictionary(pv => pv.Id);

                foreach (var a in req.SelectedAddons)
                {
                    if (dbAddonMap.TryGetValue(a.AddonProductId, out var dbVariant))
                    {
                        a.Price = (dbVariant.DiscountedPrice.HasValue && dbVariant.DiscountedPrice.Value > 0) ? dbVariant.DiscountedPrice.Value : dbVariant.Price;
                        // Enrich addon name: "BaseProduct - Variant" when names differ
                        var baseName = dbVariant.Product?.Name;
                        var variantName = dbVariant.Name;
                        if (!string.IsNullOrEmpty(baseName) && !string.IsNullOrEmpty(variantName) && baseName != variantName)
                            a.AddonName = $"{baseName} - {variantName}";
                        else if (!string.IsNullOrEmpty(baseName))
                            a.AddonName = baseName;
                        // Enrich image & stock
                        var addonImg = await _imageProcess.ResolveImage(dbVariant.Image);
                        a.NetworkImage = addonImg.NetworkUrl;
                        a.Stock = dbVariant.Stock;
                    }
                }
                addonsTotal = req.SelectedAddons.Sum(a => a.Price * a.Quantity);
                addonsJson = JsonConvert.SerializeObject(req.SelectedAddons);
            }

            // If quantity = 0 → remove item
            if (quantity == 0)
            {
                if (existingItem != null)
                    cart.Items.Remove(existingItem);
            }
            else
            {
                quantity = NormalizeQuantity(product, quantity);
                if (product.TotalAllowedQuantity < quantity)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Stock exceeded");

                var unitPrice = (product.DiscountedPrice.HasValue && product.DiscountedPrice.Value > 0) ? product.DiscountedPrice.Value : product.Price;
                var itemTotal = (quantity * unitPrice) + toppingsTotal + addonsTotal;

                if (existingItem != null)
                {
                    existingItem.Quantity = quantity;
                    existingItem.UnitPrice = unitPrice;
                    existingItem.TotalPrice = itemTotal;
                    existingItem.SelectedToppingsJson = toppingsJson;
                    existingItem.SelectedAddonsJson = addonsJson;
                    existingItem.ToppingsTotal = toppingsTotal;
                    existingItem.AddonsTotal = addonsTotal;
                }
                else
                {
                    cart.Items.Add(new CartItemDM
                    {
                        ProductVariantId = productVariantId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotal,
                        SelectedToppingsJson = toppingsJson,
                        SelectedAddonsJson = addonsJson,
                        ToppingsTotal = toppingsTotal,
                        AddonsTotal = addonsTotal,
                        DeliverySpeedType = resolvedSpeed
                    });
                }
            }

            await RecalculateCart(cart);

            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region UPDATE QUANTITY

        public async Task<CombineCartSM> UpdateCartItemQuantity(
            long userId,
            long productVariantId,
            int quantity,
            PlatformTypeSM platformSM,
            DeliverySpeedTypeSM? deliverySpeedSM = null)
        {
            var platform = (PlatformTypeDM)platformSM;
            var deliverySpeed = deliverySpeedSM.HasValue
                ? (DeliverySpeedTypeDM?)deliverySpeedSM.Value
                : null;

            var cart = await GetOrCreateCartInternal(userId, platform);

            var item = platform == PlatformTypeDM.SpeedyMart && deliverySpeed.HasValue
                ? cart.Items.FirstOrDefault(x =>
                    x.ProductVariantId == productVariantId &&
                    x.DeliverySpeedType == deliverySpeed)
                : cart.Items.FirstOrDefault(x => x.ProductVariantId == productVariantId);

            if (item == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Cart item not found");

            var product = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == productVariantId);

            if (product == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Product not found");

            if (quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                int NormalizeQuantity2(ProductVariantDM pv, int reqQty)
                {
                    int q = Math.Max(reqQty, 0);
                    var min = pv.MinOrderQty.HasValue ? (int)Math.Ceiling((double)pv.MinOrderQty.Value) : 0;
                    var max = pv.MaxOrderQty.HasValue ? (int)Math.Floor((double)pv.MaxOrderQty.Value) : int.MaxValue;
                    var step = pv.OrderStepQty.HasValue && pv.OrderStepQty.Value > 0 ? (int)Math.Ceiling((double)pv.OrderStepQty.Value) : 1;
                    if (q < min) q = min;
                    if (step > 1 && q > 0)
                    {
                        var rem = q % step;
                        if (rem != 0) q += (step - rem);
                    }
                    if (q > max)
                        throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, $"Maximum allowed quantity is {max}");
                    return q;
                }
                quantity = NormalizeQuantity2(product, quantity);
                if (product.TotalAllowedQuantity < quantity)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Stock exceeded");

                var currentUnitPrice = (product.DiscountedPrice.HasValue && product.DiscountedPrice.Value > 0) ? product.DiscountedPrice.Value : product.Price;
                item.Quantity = quantity;
                item.UnitPrice = currentUnitPrice;
                item.TotalPrice = (quantity * currentUnitPrice) + item.ToppingsTotal + item.AddonsTotal;
            }

            await RecalculateCart(cart);

            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region REMOVE ITEM

        public async Task<CombineCartSM> RemoveFromCart(
            long userId,
            long productVariantId,
            PlatformTypeSM platformSM,
            DeliverySpeedTypeSM? deliverySpeedSM = null)
        {
            var platform = (PlatformTypeDM)platformSM;
            var deliverySpeed = deliverySpeedSM.HasValue
                ? (DeliverySpeedTypeDM?)deliverySpeedSM.Value
                : null;

            var cart = await GetOrCreateCartInternal(userId, platform);

            var item = platform == PlatformTypeDM.SpeedyMart && deliverySpeed.HasValue
                ? cart.Items.FirstOrDefault(x =>
                    x.ProductVariantId == productVariantId &&
                    x.DeliverySpeedType == deliverySpeed)
                : cart.Items.FirstOrDefault(x => x.ProductVariantId == productVariantId);

            if (item != null)
                cart.Items.Remove(item);

            await RecalculateCart(cart);

            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region CLEAR CART

        public async Task<CombineCartSM> ClearCart(
            long userId,
            PlatformTypeSM platformSM)
        {
            var platform = (PlatformTypeDM)platformSM;

            var cart = await GetOrCreateCartInternal(userId, platform);

            _apiDbContext.CartItems.RemoveRange(cart.Items);

            cart.SubTotal = 0;
            cart.TaxAmount = 0;
            cart.DiscountAmount = 0;
            cart.GrandTotal = 0;

            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region PRIVATE HELPERS

        private async Task RecalculateCart(CartDM cart)
        {
            cart.SubTotal = cart.Items.Sum(x => x.TotalPrice);

            // Tax per item based on product's TaxPercentage (0 if not set)
            decimal totalTax = 0;
            var variantIds = cart.Items.Select(x => x.ProductVariantId).Distinct().ToList();
            var taxMap = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .Include(v => v.Product)
                .ToDictionaryAsync(v => v.Id, v => v.Product?.TaxPercentage ?? 0);

            foreach (var item in cart.Items)
            {
                var taxPct = taxMap.ContainsKey(item.ProductVariantId) ? taxMap[item.ProductVariantId] : 0;
                totalTax += Math.Round(item.TotalPrice * taxPct / 100m, 2);
            }

            cart.TaxAmount = totalTax;
            cart.DiscountAmount = 0;

            cart.GrandTotal = cart.SubTotal + cart.TaxAmount;

            cart.UpdatedAt = DateTime.UtcNow;
            cart.UpdatedBy = _loginUserDetail.LoginId;
        }

        public async Task<CombineCartSM> BuildCombineCartResponse(long userId)
        {
            // 1️⃣ Fetch carts with items
            var carts = await _apiDbContext.Carts
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            if (!carts.Any())
            {
                return new CombineCartSM
                {
                    HotBoxCart = null,
                    SpeedyMartCart = null,
                    HotBoxCartItems = new List<HotBoxCartItemSM>(),
                    SpeedyMartCartItems = new List<SpeedyMartCartItemSM>()
                };
            }

            var hotBoxCart = carts.FirstOrDefault(x => x.PlatformType == PlatformTypeDM.HotBox);
            var speedyMartCart = carts.FirstOrDefault(x => x.PlatformType == PlatformTypeDM.SpeedyMart);

            var response = new CombineCartSM
            {
                HotBoxCart = hotBoxCart != null ? _mapper.Map<CartSM>(hotBoxCart) : null,
                SpeedyMartCart = speedyMartCart != null ? _mapper.Map<CartSM>(speedyMartCart) : null,
                HotBoxCartItems = new List<HotBoxCartItemSM>(),
                SpeedyMartCartItems = new List<SpeedyMartCartItemSM>()
            };

            // Helper: enrich addon list with image & stock from DB
            async Task EnrichAddons(List<SelectedAddonItem>? addons)
            {
                if (addons == null || !addons.Any()) return;
                var ids = addons.Select(a => a.AddonProductId).Distinct().ToList();
                var variants = await _apiDbContext.ProductVariant.AsNoTracking()
                    .Where(pv => ids.Contains(pv.Id))
                    .Select(pv => new { pv.Id, pv.Image, pv.Stock })
                    .ToDictionaryAsync(pv => pv.Id);
                foreach (var a in addons)
                {
                    if (variants.TryGetValue(a.AddonProductId, out var av))
                    {
                        var img = await _imageProcess.ResolveImage(av.Image);
                        a.NetworkImage = img.NetworkUrl;
                        a.Stock = av.Stock;
                    }
                }
            }

            // ==============================
            // 🔥 HOTBOX SECTION
            // ==============================
            if (hotBoxCart?.Items != null && hotBoxCart.Items.Any())
            {
                var hotBoxProductIds = hotBoxCart.Items
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var hotBoxProducts = await _productVariantProcess
                    .GetHotBoxProductsByBanner(hotBoxProductIds);

                var productDictionary = hotBoxProducts
                    .ToDictionary(x => x.Id);

                foreach (var item in hotBoxCart.Items)
                {
                    productDictionary.TryGetValue(item.ProductVariantId, out var productDetails);

                    var addons = string.IsNullOrEmpty(item.SelectedAddonsJson)
                        ? null
                        : JsonConvert.DeserializeObject<List<SelectedAddonItem>>(item.SelectedAddonsJson);
                    await EnrichAddons(addons);

                    response.HotBoxCartItems.Add(new HotBoxCartItemSM
                    {
                        Id = item.Id,
                        CartId = item.CartId,
                        ProductVariantId = item.ProductVariantId,
                        HotBoxProductDetails = productDetails,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        SelectedToppings = string.IsNullOrEmpty(item.SelectedToppingsJson)
                            ? null
                            : JsonConvert.DeserializeObject<List<SelectedToppingItem>>(item.SelectedToppingsJson),
                        SelectedAddons = addons,
                        ToppingsTotal = item.ToppingsTotal,
                        AddonsTotal = item.AddonsTotal
                    });
                }
            }

            // ==============================
            // 🛒 SPEEDYMART SECTION
            // ==============================
            if (speedyMartCart?.Items != null && speedyMartCart.Items.Any())
            {
                var speedyProductIds = speedyMartCart.Items
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var speedyProducts = await _productVariantProcess
                    .GetSpeedyMartProductsByBanner(speedyProductIds);

                var productDictionary = speedyProducts
                    .ToDictionary(x => x.Id);

                // Load variant names for UnitLabel
                var variantNames = await _apiDbContext.ProductVariant
                    .AsNoTracking()
                    .Where(v => speedyProductIds.Contains(v.Id))
                    .Select(v => new { v.Id, v.Name })
                    .ToDictionaryAsync(v => v.Id, v => v.Name);

                foreach (var item in speedyMartCart.Items)
                {
                    productDictionary.TryGetValue(item.ProductVariantId, out var productDetails);
                    variantNames.TryGetValue(item.ProductVariantId, out var variantName);

                    var smItem = new SpeedyMartCartItemSM
                    {
                        Id = item.Id,
                        CartId = item.CartId,
                        ProductVariantId = item.ProductVariantId,
                        SpeedyMartProductDetails = productDetails,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        UnitLabel = variantName,
                        DeliverySpeedType = item.DeliverySpeedType.HasValue
                            ? (DeliverySpeedTypeSM)item.DeliverySpeedType.Value
                            : DeliverySpeedTypeSM.Normal
                    };

                    response.SpeedyMartCartItems.Add(smItem);
                }

                // ── Express / Normal split ────────────────────────────────────────────
                response.SpeedyMartExpressItems = response.SpeedyMartCartItems
                    .Where(x => x.DeliverySpeedType == DeliverySpeedTypeSM.Express)
                    .ToList();
                response.SpeedyMartNormalItems = response.SpeedyMartCartItems
                    .Where(x => x.DeliverySpeedType == DeliverySpeedTypeSM.Normal)
                    .ToList();

                response.SpeedyMartExpressSubTotal  = response.SpeedyMartExpressItems.Sum(x => x.TotalPrice);
                response.SpeedyMartNormalSubTotal   = response.SpeedyMartNormalItems.Sum(x => x.TotalPrice);
                response.SpeedyMartExpressItemCount = response.SpeedyMartExpressItems.Sum(x => x.Quantity);
                response.SpeedyMartNormalItemCount  = response.SpeedyMartNormalItems.Sum(x => x.Quantity);
            }

            // ── Fee info for checkout ─────────────────────────────────────────────
            var deliveryPlace = await _apiDbContext.DeliveryPlaces
                .AsNoTracking()
                .Where(x => x.Status == StatusDM.Active)
                .FirstOrDefaultAsync();
            response.DeliveryFee = deliveryPlace?.DeliveryCharges ?? 0;
            response.PlatformFee = deliveryPlace?.PlatformCharges ?? 0;
            response.FreeDeliveryThreshold = deliveryPlace?.FreeDeliveryThreshold ?? 0;

            // ── Platform-specific charges ────────────────────────────────────────
            var settings = await _settingsProcess.GetAsync();

            // HotBox charges
            if (settings != null)
            {
                var hotBoxCharges = _settingsProcess.GetChargesForPlatform(settings, PlatformTypeSM.HotBox, 1);
                response.HotBoxPlatformCharge = hotBoxCharges.PlatformCharge;
                response.HotBoxCutleryCharge = hotBoxCharges.CutleryCharge;
                response.HotBoxGiftWrapCharge = hotBoxCharges.GiftWrapCharge;
                response.HotBoxLowCartFee = hotBoxCharges.LowCartFeeCharge;

                // SpeedyMart Normal charges
                var speedyMartNormalCharges = _settingsProcess.GetChargesForPlatform(settings, PlatformTypeSM.SpeedyMart, 1);
                response.SpeedyMartNormalPlatformCharge = speedyMartNormalCharges.PlatformCharge;
                response.SpeedyMartNormalCutleryCharge = speedyMartNormalCharges.CutleryCharge;
                response.SpeedyMartNormalGiftWrapCharge = speedyMartNormalCharges.GiftWrapCharge;
                response.SpeedyMartNormalLowCartFee = speedyMartNormalCharges.LowCartFeeCharge;

                // SpeedyMart Express charges
                var speedyMartExpressCharges = _settingsProcess.GetChargesForPlatform(settings, PlatformTypeSM.SpeedyMart, 2);
                response.SpeedyMartExpressPlatformCharge = speedyMartExpressCharges.PlatformCharge;
                response.SpeedyMartExpressCutleryCharge = speedyMartExpressCharges.CutleryCharge;
                response.SpeedyMartExpressGiftWrapCharge = speedyMartExpressCharges.GiftWrapCharge;
                response.SpeedyMartExpressLowCartFee = speedyMartExpressCharges.LowCartFeeCharge;
            }

            return response;
        }

        #endregion
    }
}
/*
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class CartProcess : SiffrumBalOdataBase<CartSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ProductVariantProcess _productVariantProcess;

        public CartProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ProductVariantProcess productVariantProcess,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _productVariantProcess = productVariantProcess;
        }

        #region ODATA

        public override async Task<IQueryable<CartSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.Carts.AsNoTracking();
            return await base.MapEntityAsToQuerable<CartDM, CartSM>(_mapper, entitySet);
        }

        #endregion

        #region GET OR CREATE CART

        public async Task<CartDM> GetOrCreateCartInternal(long userId, PlatformTypeDM platform)
        {
            var cart = await _apiDbContext.Carts
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.PlatformType == platform);

            if (cart != null)
                return cart;

            cart = new CartDM
            {
                UserId = userId,
                PlatformType = platform,
                SubTotal = 0,
                TaxAmount = 0,
                DiscountAmount = 0,
                GrandTotal = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId,
                Items = new List<CartItemDM>()
            };

            await _apiDbContext.Carts.AddAsync(cart);
            await _apiDbContext.SaveChangesAsync();

            return cart;
        }

        #endregion

        #region ADD TO CART

        public async Task<CombineCartSM> AddToCart(
            long userId,
            long productVariantId,
            int quantity,
            PlatformTypeSM platformsm)
        {
            var platform = (PlatformTypeDM)platformsm;
            if (quantity <= 0)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Quantity must be greater than zero");

            using var transaction = await _apiDbContext.Database.BeginTransactionAsync();

            try
            {
                var cart = await GetOrCreateCartInternal(userId, platform);

                var product = await _apiDbContext.ProductVariant
                    .FirstOrDefaultAsync(x => x.Id == productVariantId);

                if (product == null)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Product not found");

                if (product.PlatformType != platform)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Product does not belong to selected platform");

                if (product.TotalAllowedQuantity < quantity)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Insufficient stock");

                var unitPrice = product.DiscountedPrice ?? product.Price;

                var existingItem = cart.Items
                    .FirstOrDefault(x => x.ProductVariantId == productVariantId);

                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;

                    if (product.TotalAllowedQuantity < existingItem.Quantity)
                        throw new SiffrumException(
                            ApiErrorTypeSM.InvalidInputData_NoLog,
                            "Stock exceeded");

                    existingItem.TotalPrice = existingItem.Quantity * unitPrice;
                }
                else
                {
                    cart.Items.Add(new CartItemDM
                    {
                        ProductVariantId = productVariantId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = quantity * unitPrice
                    });
                }

                await RecalculateCart(cart);

                await _apiDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return await BuildCombineCartResponse(userId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region UPDATE QUANTITY

        public async Task<CombineCartSM> UpdateCartItemQuantity(
            long userId,
            long productVariantId,
            int quantity,
            PlatformTypeSM platformSM)
        {
            var platform = (PlatformTypeDM)platformSM;
            var cart = await GetOrCreateCartInternal(userId, platform);

            var item = cart.Items
                .FirstOrDefault(x => x.ProductVariantId == productVariantId);

            if (item == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Cart item not found");

            if (quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                item.TotalPrice = quantity * item.UnitPrice;
            }

            await RecalculateCart(cart);
            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region REMOVE ITEM

        public async Task<CombineCartSM> RemoveFromCart(
            long userId,
            long productVariantId,
            PlatformTypeSM platformSM)
        {
            var platform = (PlatformTypeDM)platformSM;
            var cart = await GetOrCreateCartInternal(userId, platform);

            var item = cart.Items
                .FirstOrDefault(x => x.ProductVariantId == productVariantId);

            if (item != null)
                cart.Items.Remove(item);

            await RecalculateCart(cart);
            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region CLEAR CART

        public async Task<CombineCartSM> ClearCart(
            long userId,
            PlatformTypeSM platformSM)
        {
            var platform = (PlatformTypeDM)platformSM;
            var cart = await GetOrCreateCartInternal(userId, platform);

            _apiDbContext.CartItems.RemoveRange(cart.Items);

            cart.SubTotal = 0;
            cart.TaxAmount = 0;
            cart.DiscountAmount = 0;
            cart.GrandTotal = 0;

            await _apiDbContext.SaveChangesAsync();

            return await BuildCombineCartResponse(userId);
        }

        #endregion

        #region PRIVATE HELPERS

        private async Task RecalculateCart(CartDM cart)
        {
            cart.SubTotal = cart.Items.Sum(x => x.TotalPrice);

            // Tax per item based on product's TaxPercentage (0 if not set)
            decimal totalTax = 0;
            var variantIds = cart.Items.Select(x => x.ProductVariantId).Distinct().ToList();
            var taxMap = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .Include(v => v.Product)
                .ToDictionaryAsync(v => v.Id, v => v.Product?.TaxPercentage ?? 0);

            foreach (var item in cart.Items)
            {
                var taxPct = taxMap.ContainsKey(item.ProductVariantId) ? taxMap[item.ProductVariantId] : 0;
                totalTax += Math.Round(item.TotalPrice * taxPct / 100m, 2);
            }

            cart.TaxAmount = totalTax;
            cart.DiscountAmount = 0;
            cart.GrandTotal = cart.SubTotal + cart.TaxAmount;
            
            cart.UpdatedAt = DateTime.UtcNow;
            cart.UpdatedBy = _loginUserDetail.LoginId;
        }

        public async Task<CombineCartSM> BuildCombineCartResponse(long userId)
        {
            // 1️⃣ Fetch carts with items
            var carts = await _apiDbContext.Carts
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => x.UserId == userId)
                .ToListAsync();

            if (!carts.Any())
            {
                return new CombineCartSM
                {
                    HotBoxCart = null,
                    SpeedyMartCart = null,
                    HotBoxCartItems = new List<HotBoxCartItemSM>(),
                    SpeedyMartCartItems = new List<SpeedyMartCartItemSM>()
                };
            }

            var hotBoxCart = carts.FirstOrDefault(x => x.PlatformType == PlatformTypeDM.HotBox);
            var speedyMartCart = carts.FirstOrDefault(x => x.PlatformType == PlatformTypeDM.SpeedyMart);

            var response = new CombineCartSM
            {
                HotBoxCart = hotBoxCart != null ? _mapper.Map<CartSM>(hotBoxCart) : null,
                SpeedyMartCart = speedyMartCart != null ? _mapper.Map<CartSM>(speedyMartCart) : null,
                HotBoxCartItems = new List<HotBoxCartItemSM>(),
                SpeedyMartCartItems = new List<SpeedyMartCartItemSM>()
            };

            // ==============================
            // 🔥 HOTBOX SECTION
            // ==============================
            if (hotBoxCart?.Items != null && hotBoxCart.Items.Any())
            {
                var hotBoxProductIds = hotBoxCart.Items
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var hotBoxProducts = await _productVariantProcess
                    .GetHotBoxProductsByBanner(hotBoxProductIds);

                var productDictionary = hotBoxProducts
                    .ToDictionary(x => x.Id);

                foreach (var item in hotBoxCart.Items)
                {
                    productDictionary.TryGetValue(item.ProductVariantId, out var productDetails);

                    response.HotBoxCartItems.Add(new HotBoxCartItemSM
                    {
                        Id = item.Id,
                        CartId = item.CartId,
                        ProductVariantId = item.ProductVariantId,
                        HotBoxProductDetails = productDetails,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        SelectedToppings = string.IsNullOrEmpty(item.SelectedToppingsJson)
                            ? null
                            : JsonConvert.DeserializeObject<List<SelectedToppingItem>>(item.SelectedToppingsJson),
                        SelectedAddons = string.IsNullOrEmpty(item.SelectedAddonsJson)
                            ? null
                            : JsonConvert.DeserializeObject<List<SelectedAddonItem>>(item.SelectedAddonsJson),
                        ToppingsTotal = item.ToppingsTotal,
                        AddonsTotal = item.AddonsTotal
                    });
                }
            }

            // ==============================
            // 🛒 SPEEDYMART SECTION
            // ==============================
            if (speedyMartCart?.Items != null && speedyMartCart.Items.Any())
            {
                var speedyProductIds = speedyMartCart.Items
                    .Select(x => x.ProductVariantId)
                    .Distinct()
                    .ToList();

                var speedyProducts = await _productVariantProcess
                    .GetSpeedyMartProductsByBanner(speedyProductIds);

                var productDictionary = speedyProducts
                    .ToDictionary(x => x.Id);

                foreach (var item in speedyMartCart.Items)
                {
                    productDictionary.TryGetValue(item.ProductVariantId, out var productDetails);

                    response.SpeedyMartCartItems.Add(new SpeedyMartCartItemSM
                    {
                        Id = item.Id,
                        CartId = item.CartId,
                        ProductVariantId = item.ProductVariantId,
                        SpeedyMartProductDetails = productDetails, // safe null
                        Quantity = item.Quantity,                  // ✅ FROM CART ITEM
                        UnitPrice = item.UnitPrice,                // ✅ FROM CART ITEM
                        TotalPrice = item.TotalPrice               // ✅ FROM CART ITEM
                    });
                }
            }

            return response;
        }

        #endregion
    }
}*/