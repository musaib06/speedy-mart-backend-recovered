using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class WishlistProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ImageProcess _imageProcess;

        public WishlistProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail,
            ImageProcess imageProcess)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _imageProcess = imageProcess;
        }

        public async Task<List<WishlistItemSM>> GetWishlistAsync(long userId, int skip, int top, int? deliverySpeedType = null)
        {
            var query = _apiDbContext.WishlistItems
                .AsNoTracking()
                .Where(w => w.UserId == userId);

            if (deliverySpeedType.HasValue && deliverySpeedType.Value != 3)
            {
                // 1=Normal, 2=Express — also include items marked as Both(3)
                query = query.Where(w => w.ProductVariant.DeliverySpeedType == deliverySpeedType.Value
                                      || w.ProductVariant.DeliverySpeedType == 3);
            }

            var items = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip(skip)
                .Take(top)
                .Include(w => w.ProductVariant)
                    .ThenInclude(v => v.Product)
                .ToListAsync();

            var result = new List<WishlistItemSM>();
            foreach (var item in items)
            {
                var v = item.ProductVariant;
                var img = await _imageProcess.ResolveImage(v?.Image);

                result.Add(new WishlistItemSM
                {
                    Id = item.Id,
                    ProductVariantId = item.ProductVariantId,
                    ProductName = v?.Product?.Name ?? v?.Name ?? "",
                    VariantName = v?.Name ?? "",
                    ImageBase64 = img.Base64,
                    NetworkImage = img.NetworkUrl,
                    Price = v?.Price ?? 0,
                    DiscountedPrice = v?.DiscountedPrice ?? 0,
                    IsInStock = v != null && v.Status == ProductStatusDM.Active && v.Stock > 0,
                    Stock = v?.Stock ?? 0,
                    UnitLabel = v?.Name ?? "",
                    DeliverySpeedType = v?.DeliverySpeedType ?? 1,
                    CreatedAt = item.CreatedAt
                });
            }
            return result;
        }

        public async Task<IntResponseRoot> GetWishlistCountAsync(long userId, int? deliverySpeedType = null)
        {
            var query = _apiDbContext.WishlistItems
                .AsNoTracking()
                .Where(w => w.UserId == userId);

            if (deliverySpeedType.HasValue && deliverySpeedType.Value != 3)
            {
                query = query.Where(w => w.ProductVariant.DeliverySpeedType == deliverySpeedType.Value
                                      || w.ProductVariant.DeliverySpeedType == 3);
            }

            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total Wishlist Items");
        }

        public async Task<WishlistItemSM> AddToWishlistAsync(long userId, long productVariantId)
        {
            // Check if already in wishlist
            var existing = await _apiDbContext.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductVariantId == productVariantId);

            if (existing != null)
            {
                // Already exists — just return it
                return await GetSingleItemAsync(existing);
            }

            // Verify variant exists
            var variant = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == productVariantId);

            if (variant == null)
                throw new Exception("Product variant not found");

            var item = new WishlistItemDM
            {
                UserId = userId,
                ProductVariantId = productVariantId,
                CreatedAt = DateTime.UtcNow
            };

            _apiDbContext.WishlistItems.Add(item);
            await _apiDbContext.SaveChangesAsync();

            return await GetSingleItemAsync(item);
        }

        public async Task<BoolResponseRoot> RemoveFromWishlistAsync(long userId, long productVariantId)
        {
            var item = await _apiDbContext.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductVariantId == productVariantId);

            if (item == null)
                return new BoolResponseRoot(false, "Item not found in wishlist");

            _apiDbContext.WishlistItems.Remove(item);
            await _apiDbContext.SaveChangesAsync();
            return new BoolResponseRoot(true, "Removed from wishlist");
        }

        public async Task<BoolResponseRoot> IsInWishlistAsync(long userId, long productVariantId)
        {
            var exists = await _apiDbContext.WishlistItems
                .AsNoTracking()
                .AnyAsync(w => w.UserId == userId && w.ProductVariantId == productVariantId);
            return new BoolResponseRoot(exists, exists ? "In wishlist" : "Not in wishlist");
        }

        private async Task<WishlistItemSM> GetSingleItemAsync(WishlistItemDM item)
        {
            var v = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.Id == item.ProductVariantId);

            var img = await _imageProcess.ResolveImage(v?.Image);

            return new WishlistItemSM
            {
                Id = item.Id,
                ProductVariantId = item.ProductVariantId,
                ProductName = v?.Product?.Name ?? v?.Name ?? "",
                VariantName = v?.Name ?? "",
                ImageBase64 = img.Base64,
                NetworkImage = img.NetworkUrl,
                Price = v?.Price ?? 0,
                DiscountedPrice = v?.DiscountedPrice ?? 0,
                IsInStock = v != null && v.Status == ProductStatusDM.Active && v.Stock > 0,
                Stock = v?.Stock ?? 0,
                UnitLabel = v?.Name ?? "",
                DeliverySpeedType = v?.DeliverySpeedType ?? 1,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
