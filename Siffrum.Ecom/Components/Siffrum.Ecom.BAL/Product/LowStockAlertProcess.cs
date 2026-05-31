using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class LowStockAlertProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public LowStockAlertProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        public async Task<List<LowStockAlertSM>> GetBySellerAsync(long sellerId, bool activeOnly = true)
        {
            var query = _apiDbContext.LowStockAlerts
                .AsNoTracking()
                .Include(x => x.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Where(x => x.SellerId == sellerId);

            if (activeOnly)
                query = query.Where(x => x.IsActive);

            var dms = await query.OrderByDescending(x => x.CreatedAt).ToListAsync();

            return dms.Select(dm => new LowStockAlertSM
            {
                Id = dm.Id,
                ProductVariantId = dm.ProductVariantId,
                VariantName = dm.ProductVariant?.Name,
                ProductName = dm.ProductVariant?.Product?.Name,
                SellerId = dm.SellerId,
                ThresholdQuantity = dm.ThresholdQuantity,
                IsActive = dm.IsActive,
                LastAlertSentAt = dm.LastAlertSentAt,
                CurrentStock = dm.ProductVariant?.Stock
            }).ToList();
        }

        public async Task<LowStockAlertSM> CreateAsync(LowStockAlertSM sm)
        {
            var variant = await _apiDbContext.ProductVariant.FindAsync(sm.ProductVariantId);
            if (variant == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Product variant not found");

            var existing = await _apiDbContext.LowStockAlerts
                .AnyAsync(x => x.ProductVariantId == sm.ProductVariantId && x.SellerId == sm.SellerId && x.IsActive);
            if (existing)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Alert already exists for this variant");

            var dm = new LowStockAlertDM
            {
                ProductVariantId = sm.ProductVariantId,
                SellerId = sm.SellerId,
                ThresholdQuantity = sm.ThresholdQuantity > 0 ? sm.ThresholdQuantity : 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            };

            _apiDbContext.LowStockAlerts.Add(dm);
            await _apiDbContext.SaveChangesAsync();

            return _mapper.Map<LowStockAlertSM>(dm);
        }

        public async Task<BoolResponseRoot> DeactivateAsync(long id)
        {
            var dm = await _apiDbContext.LowStockAlerts.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Alert not found");

            dm.IsActive = false;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;
            await _apiDbContext.SaveChangesAsync();

            return new BoolResponseRoot(true, "Alert deactivated");
        }

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var dm = await _apiDbContext.LowStockAlerts.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Alert not found");

            _apiDbContext.LowStockAlerts.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new DeleteResponseRoot(true, "Alert deleted");
        }

        // Called after inventory changes to check thresholds
        public async Task CheckAndTriggerAlerts(long productVariantId)
        {
            var variant = await _apiDbContext.ProductVariant.FindAsync(productVariantId);
            if (variant == null) return;

            var alerts = await _apiDbContext.LowStockAlerts
                .Where(x => x.ProductVariantId == productVariantId && x.IsActive)
                .ToListAsync();

            foreach (var alert in alerts)
            {
                if (variant.Stock <= alert.ThresholdQuantity)
                {
                    alert.LastAlertSentAt = DateTime.UtcNow;
                    // TODO: Send push notification to seller
                }
            }

            await _apiDbContext.SaveChangesAsync();
        }
    }
}
