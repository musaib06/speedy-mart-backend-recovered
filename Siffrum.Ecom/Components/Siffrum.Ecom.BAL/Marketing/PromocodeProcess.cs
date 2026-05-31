using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Marketing
{
    public class PromocodeProcess : SiffrumBalOdataBase<PromoCodeSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public PromocodeProcess(
            ApiDbContext apiDbContext,
            IMapper mapper,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        #region ODATA
        public override async Task<IQueryable<PromoCodeSM>> GetServiceModelEntitiesForOdata()
        {
            var entitySet = _apiDbContext.PromoCodes.AsNoTracking();
            return await base.MapEntityAsToQuerable<PromoCodeDM, PromoCodeSM>(_mapper, entitySet);
        }
        #endregion

        #region CREATE
        public async Task<BoolResponseRoot> CreateAsync(PromoCodeSM objSM)
        {
            if (objSM == null)
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Promo code data is required");

            bool exists = await _apiDbContext.PromoCodes
                .AnyAsync(x => x.Code.ToLower() == objSM.Code.ToLower());

            if (exists)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Promo code already exists");

            var dm = _mapper.Map<PromoCodeDM>(objSM);

            dm.Code = dm.Code.Trim().ToUpper();
            dm.UsedCount = 0;
            dm.CreatedAt = DateTime.UtcNow;
            dm.CreatedBy = _loginUserDetail.LoginId;

            await _apiDbContext.PromoCodes.AddAsync(dm);

            if (await _apiDbContext.SaveChangesAsync() > 0)
                return new BoolResponseRoot(true, "Promo code created successfully");

            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, "Failed to create promo code");
        }
        #endregion

        #region READ

        public async Task<PromoCodeSM?> GetByIdAsync(long id)
        {
            var dm = await _apiDbContext.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return dm == null ? null : _mapper.Map<PromoCodeSM>(dm);
        }

        public async Task<PromoCodeSM?> GetByCodeAsync(string code)
        {
            var dm = await _apiDbContext.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code.ToUpper());

            return dm == null ? null : _mapper.Map<PromoCodeSM>(dm);
        }

        public async Task<List<PromoCodeSM>> GetAll(int skip, int top)
        {
            var list = await _apiDbContext.PromoCodes
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            return _mapper.Map<List<PromoCodeSM>>(list);
        }

        public async Task<IntResponseRoot> GetCount()
        {
            var count = await _apiDbContext.PromoCodes
                .CountAsync();
            return new IntResponseRoot(count, "Total Promo Codes");
        }

        #endregion

        #region UPDATE

        public async Task<PromoCodeSM?> UpdateAsync(long id, PromoCodeSM objSM)
        {
            var dm = await _apiDbContext.PromoCodes
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Promo code not found");

            dm.Type = (CouponTypeDM)objSM.Type;
            dm.DiscountValue = objSM.DiscountValue;
            dm.MaxDiscountAmount = objSM.MaxDiscountAmount;
            dm.MinimumCartAmount = objSM.MinimumCartAmount;
            dm.UsageLimit = objSM.UsageLimit;
            dm.UsagePerUserLimit = objSM.UsagePerUserLimit;
            dm.IsActive = objSM.IsActive;
            dm.IsFirstOrderOnly = objSM.IsFirstOrderOnly;
            dm.PlatformType = (PlatformTypeDM)objSM.PlatformType;
            dm.ApplicableDeliverySpeed = objSM.ApplicableDeliverySpeed.HasValue
                ? (DeliverySpeedTypeDM)objSM.ApplicableDeliverySpeed.Value
                : null;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            await _apiDbContext.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<BoolResponseRoot> UpdateStatusAsync(long id, bool isActive)
        {
            var dm = await _apiDbContext.PromoCodes
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Promo code not found");
            if(dm.IsActive == isActive)
            {
                return new BoolResponseRoot(true, "Promo code status already updated");
            }
            dm.IsActive = isActive;

            if (await _apiDbContext.SaveChangesAsync() > 0)
                return new BoolResponseRoot(true, "Promo code status updated");

            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, "Failed to update status");
        }

        #endregion

        #region DELETE

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var dm = await _apiDbContext.PromoCodes
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Promo code not found");

            _apiDbContext.PromoCodes.Remove(dm);

            if (await _apiDbContext.SaveChangesAsync() > 0)
                return new DeleteResponseRoot(true, "Promo code deleted successfully");

            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, "Failed to delete promo code");
        }

        #endregion

        #region VALIDATE / APPLY PROMO CODE

        public async Task<PromoCodeValidationResultSM> ValidatePromoCodeAsync(
            string code,
            decimal cartSubtotal,
            long userId,
            PlatformTypeSM? platform = null,
            DeliverySpeedTypeSM? deliverySpeed = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Promo code is required");

            var promo = await _apiDbContext.PromoCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code.Trim().ToUpper());

            if (promo == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Invalid promo code");

            if (!promo.IsActive)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "This promo code is no longer active");

            if (promo.PlatformType.HasValue && platform.HasValue
                && promo.PlatformType != (PlatformTypeDM)platform.Value)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "This promo code is not valid for this platform");

            if (promo.ApplicableDeliverySpeed.HasValue && deliverySpeed.HasValue
                && promo.ApplicableDeliverySpeed != (DeliverySpeedTypeDM)deliverySpeed.Value
                && promo.ApplicableDeliverySpeed != DeliverySpeedTypeDM.Both)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "This promo code is not valid for this delivery type");

            if (promo.MinimumCartAmount.HasValue && cartSubtotal < promo.MinimumCartAmount.Value)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    $"Minimum order amount of ₹{promo.MinimumCartAmount.Value:F0} is required. Your cart is ₹{cartSubtotal:F0}");

            if (promo.UsageLimit.HasValue && (promo.UsedCount ?? 0) >= promo.UsageLimit.Value)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "This promo code has reached its usage limit");

            // Check per-user usage
            var userUsage = await _apiDbContext.UserPromocodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.PromocodeId == promo.Id);

            var perUserLimit = promo.UsagePerUserLimit ?? 1; // default: 1 use per user
            if (userUsage != null && userUsage.UsageCount >= perUserLimit)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "You have already used this promo code");

            // Check first-order-only
            if (promo.IsFirstOrderOnly)
            {
                var hasOrders = await _apiDbContext.Order.AsNoTracking()
                    .AnyAsync(o => o.UserId == userId);
                if (hasOrders)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "This promo code is valid for first orders only");
            }

            // Calculate discount
            decimal discount;
            if (promo.Type == CouponTypeDM.Percentage)
            {
                discount = cartSubtotal * (promo.DiscountValue / 100m);
                if (promo.MaxDiscountAmount.HasValue)
                    discount = Math.Min(discount, promo.MaxDiscountAmount.Value);
            }
            else
            {
                discount = Math.Min(promo.DiscountValue, cartSubtotal);
            }

            discount = Math.Round(discount, 2);

            return new PromoCodeValidationResultSM
            {
                IsValid = true,
                PromoCodeId = promo.Id,
                Code = promo.Code,
                DiscountType = ((CouponTypeSM)promo.Type).ToString(),
                DiscountValue = promo.DiscountValue,
                DiscountAmount = discount,
                FinalAmount = Math.Max(0, cartSubtotal - discount),
                Message = $"Promo code applied! You save ₹{discount:F0}"
            };
        }

        public async Task RecordUsageAsync(long promoCodeId, long userId)
        {
            var userPromo = await _apiDbContext.UserPromocodes
                .FirstOrDefaultAsync(x => x.PromocodeId == promoCodeId && x.UserId == userId);

            if (userPromo == null)
            {
                userPromo = new UserPromocodesDM
                {
                    PromocodeId = promoCodeId,
                    UserId = userId,
                    UsageCount = 1,
                    CreatedAt = DateTime.UtcNow
                };
                await _apiDbContext.UserPromocodes.AddAsync(userPromo);
            }
            else
            {
                userPromo.UsageCount += 1;
                userPromo.UpdatedAt = DateTime.UtcNow;
            }

            // Increment global used count
            var promo = await _apiDbContext.PromoCodes.FindAsync(promoCodeId);
            if (promo != null)
                promo.UsedCount = (promo.UsedCount ?? 0) + 1;

            await _apiDbContext.SaveChangesAsync();
        }

        #endregion
    }
}
