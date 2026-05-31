using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class InventoryTransactionProcess : SiffrumBalBase
    {
        public InventoryTransactionProcess(IMapper mapper, ApiDbContext apiDbContext)
            : base(mapper, apiDbContext) { }

        public async Task LogAsync(
            long productVariantId,
            long? sellerId,
            string changeType,
            decimal? quantityBefore,
            decimal? quantityAfter,
            decimal delta,
            long? referenceId = null,
            string? note = null)
        {
            try
            {
                var tx = new InventoryTransactionDM
                {
                    ProductVariantId = productVariantId,
                    SellerId = sellerId,
                    ChangeType = changeType,
                    QuantityBefore = quantityBefore,
                    QuantityAfter = quantityAfter,
                    Delta = delta,
                    ReferenceId = referenceId,
                    Note = note,
                    CreatedAt = DateTime.UtcNow
                };
                await _apiDbContext.InventoryTransactions.AddAsync(tx);
                await _apiDbContext.SaveChangesAsync();
            }
            catch { /* never fail the caller */ }
        }

        public async Task<List<InventoryTransactionSM>> GetAdminTransactionsAsync(
            long? sellerId,
            long? variantId,
            string? changeType,
            DateTime? dateFrom,
            DateTime? dateTo,
            int skip,
            int top)
        {
            var query = _apiDbContext.InventoryTransactions
                .AsNoTracking()
                .Include(t => t.ProductVariant)
                    .ThenInclude(v => v.Product)
                .AsQueryable();

            if (sellerId.HasValue && sellerId.Value > 0)
                query = query.Where(t => t.SellerId == sellerId.Value);

            if (variantId.HasValue && variantId.Value > 0)
                query = query.Where(t => t.ProductVariantId == variantId.Value);

            if (!string.IsNullOrWhiteSpace(changeType) && changeType != "all")
                query = query.Where(t => t.ChangeType == changeType);

            if (dateFrom.HasValue)
                query = query.Where(t => t.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(t => t.CreatedAt <= dateTo.Value);

            var rows = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            var sellerIds = rows.Where(r => r.SellerId.HasValue).Select(r => r.SellerId!.Value).Distinct().ToList();
            var sellerNames = await _apiDbContext.Seller
                .AsNoTracking()
                .Where(s => sellerIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.StoreName ?? s.Name ?? "");

            return rows.Select(t => new InventoryTransactionSM
            {
                Id = t.Id,
                ProductVariantId = t.ProductVariantId,
                SellerId = t.SellerId,
                SellerName = t.SellerId.HasValue && sellerNames.TryGetValue(t.SellerId.Value, out var sn) ? sn : null,
                ProductName = t.ProductVariant?.Product?.Name,
                VariantName = t.ProductVariant?.Name,
                PlatformType = t.ProductVariant?.PlatformType.ToString(),
                ChangeType = t.ChangeType,
                QuantityBefore = t.QuantityBefore,
                QuantityAfter = t.QuantityAfter,
                Delta = t.Delta,
                ReferenceId = t.ReferenceId,
                Note = t.Note,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<IntResponseRoot> GetAdminTransactionsCountAsync(
            long? sellerId,
            long? variantId,
            string? changeType,
            DateTime? dateFrom,
            DateTime? dateTo)
        {
            var query = _apiDbContext.InventoryTransactions.AsNoTracking().AsQueryable();

            if (sellerId.HasValue && sellerId.Value > 0)
                query = query.Where(t => t.SellerId == sellerId.Value);

            if (variantId.HasValue && variantId.Value > 0)
                query = query.Where(t => t.ProductVariantId == variantId.Value);

            if (!string.IsNullOrWhiteSpace(changeType) && changeType != "all")
                query = query.Where(t => t.ChangeType == changeType);

            if (dateFrom.HasValue)
                query = query.Where(t => t.CreatedAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(t => t.CreatedAt <= dateTo.Value);

            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total");
        }

        public async Task<List<InventoryTransactionSM>> GetAdminLowStockAsync(
            long? sellerId,
            string? platform,
            int threshold = 10)
        {
            var query = _apiDbContext.ProductVariant
                .AsNoTracking()
                .Include(v => v.Product)
                .Where(v => v.Stock.HasValue && v.Stock <= threshold && v.Stock > 0 && !v.IsUnlimitedStock);

            if (sellerId.HasValue && sellerId.Value > 0)
                query = query.Where(v => v.Product.SellerId == sellerId.Value);

            if (!string.IsNullOrWhiteSpace(platform) && platform != "all" &&
                Enum.TryParse<DomainModels.Enums.PlatformTypeDM>(platform, true, out var pt) &&
                pt != DomainModels.Enums.PlatformTypeDM.None)
                query = query.Where(v => v.PlatformType == pt);

            var variants = await query
                .OrderBy(v => v.Stock)
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.Stock,
                    v.Price,
                    v.PlatformType,
                    ProductName = v.Product.Name,
                    CategoryName = v.Product.Category != null ? v.Product.Category.Name : null,
                    SellerId = v.Product.SellerId,
                })
                .ToListAsync();

            var sellerIdList = variants.Select(v => v.SellerId).Distinct().ToList();
            var sellerNames = await _apiDbContext.Seller
                .AsNoTracking()
                .Where(s => sellerIdList.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.StoreName ?? s.Name ?? "");

            return variants.Select(v => new InventoryTransactionSM
            {
                ProductVariantId = v.Id,
                SellerId = v.SellerId,
                SellerName = sellerNames.TryGetValue(v.SellerId, out var sn) ? sn : null,
                ProductName = v.ProductName,
                VariantName = v.Name,
                PlatformType = v.PlatformType.ToString(),
                QuantityAfter = v.Stock,
                Delta = v.Stock ?? 0,
                Note = v.CategoryName,
                ChangeType = "LowStock"
            }).ToList();
        }
    }
}
