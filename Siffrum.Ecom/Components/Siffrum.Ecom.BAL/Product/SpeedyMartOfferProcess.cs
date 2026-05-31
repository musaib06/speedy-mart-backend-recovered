using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class SpeedyMartOfferProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public SpeedyMartOfferProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        public async Task<List<SpeedyMartOfferSM>> GetAllAsync(
            DeliverySpeedTypeDM? deliverySpeed = null,
            bool? activeOnly = true,
            int skip = 0, int top = 50)
        {
            var query = _apiDbContext.SpeedyMartOffers.AsNoTracking().AsQueryable();

            if (deliverySpeed.HasValue && deliverySpeed.Value != DeliverySpeedTypeDM.Both)
                query = query.Where(x =>
                    x.ApplicableDeliverySpeed == deliverySpeed.Value ||
                    x.ApplicableDeliverySpeed == DeliverySpeedTypeDM.Both);

            if (activeOnly == true)
                query = query.Where(x => x.IsActive);

            var dms = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            return _mapper.Map<List<SpeedyMartOfferSM>>(dms);
        }

        public async Task<SpeedyMartOfferSM> GetByIdAsync(long id)
        {
            var dm = await _apiDbContext.SpeedyMartOffers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Offer not found");

            return _mapper.Map<SpeedyMartOfferSM>(dm);
        }

        public async Task<SpeedyMartOfferSM> CreateAsync(SpeedyMartOfferSM sm)
        {
            if (string.IsNullOrWhiteSpace(sm.Title))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Title is required");
            if (sm.DiscountValue <= 0)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Discount value must be positive");

            var dm = _mapper.Map<SpeedyMartOfferDM>(sm);
            dm.PlatformType = PlatformTypeDM.SpeedyMart;
            dm.CreatedAt = DateTime.UtcNow;
            dm.CreatedBy = _loginUserDetail.LoginId;

            _apiDbContext.SpeedyMartOffers.Add(dm);
            await _apiDbContext.SaveChangesAsync();

            return _mapper.Map<SpeedyMartOfferSM>(dm);
        }

        public async Task<SpeedyMartOfferSM> UpdateAsync(long id, SpeedyMartOfferSM sm)
        {
            var dm = await _apiDbContext.SpeedyMartOffers.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Offer not found");

            if (string.IsNullOrWhiteSpace(sm.Title))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Title is required");

            dm.Title = sm.Title.Trim();
            dm.Description = sm.Description?.Trim();
            dm.OfferType = sm.OfferType;
            dm.DiscountType = sm.DiscountType;
            dm.DiscountValue = sm.DiscountValue;
            dm.ApplicableDeliverySpeed = (DeliverySpeedTypeDM)sm.ApplicableDeliverySpeed;
            dm.MinOrderValue = sm.MinOrderValue;
            dm.MaxDiscount = sm.MaxDiscount;
            dm.TargetId = sm.TargetId;
            dm.OfferCode = sm.OfferCode?.Trim();
            dm.ValidFrom = sm.ValidFrom;
            dm.ValidTo = sm.ValidTo;
            dm.IsActive = sm.IsActive;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            await _apiDbContext.SaveChangesAsync();
            return _mapper.Map<SpeedyMartOfferSM>(dm);
        }

        public async Task<BoolResponseRoot> ToggleActiveAsync(long id)
        {
            var dm = await _apiDbContext.SpeedyMartOffers.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Offer not found");

            dm.IsActive = !dm.IsActive;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;
            await _apiDbContext.SaveChangesAsync();

            return new BoolResponseRoot(true, $"Offer {(dm.IsActive ? "activated" : "deactivated")}");
        }

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var dm = await _apiDbContext.SpeedyMartOffers.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Offer not found");

            _apiDbContext.SpeedyMartOffers.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new DeleteResponseRoot(true, "Offer deleted");
        }
    }
}
