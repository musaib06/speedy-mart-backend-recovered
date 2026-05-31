using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Base.Email;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.AppUser;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;
using Siffrum.Ecom.ServiceModels.v1.General;
using System.Text;
using Siffrum.Ecom.DomainModels.Enums;
using Newtonsoft.Json;

namespace Siffrum.Ecom.BAL.LoginUsers
{
    public class SettingsProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public SettingsProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        #region Helpers (JSON)

        private SettingsSM Deserialize(string json)
        {
            return string.IsNullOrEmpty(json)
                ? new SettingsSM()
                : JsonConvert.DeserializeObject<SettingsSM>(json);
        }

        private string Serialize(SettingsSM sm)
        {
            return JsonConvert.SerializeObject(sm);
        }

        #endregion

        #region CREATE (Only One Row Allowed)

        public async Task<SettingsSM> CreateAsync(SettingsSM objSM)
        {
            if (objSM == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.ModelError_NoLog,
                    "Settings data is required");

            var exists = await _apiDbContext.Settings.AnyAsync();

            if (exists)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Settings already exist. Use update.");

            var dm = new SettingsDM
            {
                JsonData = Serialize(objSM),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            };
            await _apiDbContext.Settings.AddAsync(dm);

            if (await _apiDbContext.SaveChangesAsync() > 0)
                return await GetAsync();

            throw new SiffrumException(
                ApiErrorTypeSM.Fatal_Log,
                "Failed to create settings");
        }

        #endregion

        #region READ

        public async Task<SettingsSM> GetAsync()
        {
            var dm = await _apiDbContext.Settings
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (dm == null)
                return new SettingsSM(); // default empty

            var sm = Deserialize(dm.JsonData);
            sm.Id = dm.Id;

            return sm;
        }

        #endregion

        #region UPDATE

        public async Task<SettingsSM> UpdateAsync(SettingsSM objSM)
        {
            if (objSM == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.ModelError_NoLog,
                    "Settings data is required");

            var dm = await _apiDbContext.Settings
                .FirstOrDefaultAsync();

            if (dm == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Settings not found. Create first.");

            dm.JsonData = Serialize(objSM);
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            await _apiDbContext.SaveChangesAsync();

            var result = Deserialize(dm.JsonData);
            result.Id = dm.Id;

            return result;
        }

        #endregion

        #region Platform-Specific Charges

        /// <summary>
        /// Get charge configuration for a specific platform and delivery speed
        /// </summary>
        public async Task<PlatformChargeConfig> GetChargesForPlatformAsync(PlatformTypeSM platformType, int deliverySpeedType = 1)
        {
            var settings = await GetAsync();
            return GetChargesForPlatform(settings, platformType, deliverySpeedType);
        }

        /// <summary>
        /// Get charge configuration from settings object
        /// </summary>
        public PlatformChargeConfig GetChargesForPlatform(SettingsSM settings, PlatformTypeSM platformType, int deliverySpeedType = 1)
        {
            // Check if new platform-specific config exists and has values
            if (platformType == PlatformTypeSM.HotBox)
            {
                if (settings.HotBoxCharges != null && HasAnyChargeValue(settings.HotBoxCharges))
                    return settings.HotBoxCharges;
            }
            else if (platformType == PlatformTypeSM.SpeedyMart)
            {
                // SpeedyMart: 1=Normal, 2=Express
                if (deliverySpeedType == 2) // Express
                {
                    if (settings.SpeedyMartExpressCharges != null && HasAnyChargeValue(settings.SpeedyMartExpressCharges))
                        return settings.SpeedyMartExpressCharges;
                }
                else // Normal (default)
                {
                    if (settings.SpeedyMartNormalCharges != null && HasAnyChargeValue(settings.SpeedyMartNormalCharges))
                        return settings.SpeedyMartNormalCharges;
                }
            }

            // Fallback to legacy values if platform-specific not configured
            return new PlatformChargeConfig
            {
                PlatformCharge = settings.PlatormCharge,
                CutleryCharge = settings.CutlaryCharge,
                GiftWrapCharge = settings.GiftWrapCharge,
                LowCartFeeCharge = settings.LowCartFeeCharge,
                LowCartAmountValue = settings.LowCartAmountValue
            };
        }

        /// <summary>
        /// Calculate all charges for an order/cart
        /// </summary>
        public async Task<(decimal platformCharge, decimal cutleryCharge, decimal giftWrapCharge, decimal lowCartFee)> 
            CalculateChargesAsync(decimal subTotal, PlatformTypeSM platformType, int deliverySpeedType, bool hasCutlery, bool hasGiftWrap)
        {
            var settings = await GetAsync();
            var charges = GetChargesForPlatform(settings, platformType, deliverySpeedType);

            // Platform charge is always applied
            decimal platformCharge = charges.PlatformCharge;

            // Cutlery charge only if applicable (NOT for SpeedyMart - food delivery only)
            decimal cutleryCharge = (hasCutlery && platformType != PlatformTypeSM.SpeedyMart) ? charges.CutleryCharge : 0;

            // Gift wrap charge only if applicable  
            decimal giftWrapCharge = hasGiftWrap ? charges.GiftWrapCharge : 0;

            // Low cart fee if subtotal is below threshold
            decimal lowCartFee = 0;
            if (charges.LowCartAmountValue > 0 && subTotal < charges.LowCartAmountValue)
            {
                lowCartFee = charges.LowCartFeeCharge;
            }

            return (platformCharge, cutleryCharge, giftWrapCharge, lowCartFee);
        }

        /// <summary>
        /// Calculate total additional charges
        /// </summary>
        public async Task<decimal> CalculateTotalChargesAsync(decimal subTotal, PlatformTypeSM platformType, int deliverySpeedType, bool hasCutlery, bool hasGiftWrap)
        {
            var (platform, cutlery, giftWrap, lowCart) = await CalculateChargesAsync(subTotal, platformType, deliverySpeedType, hasCutlery, hasGiftWrap);
            return platform + cutlery + giftWrap + lowCart;
        }

        private bool HasAnyChargeValue(PlatformChargeConfig config)
        {
            if (config == null) return false;
            return config.PlatformCharge > 0 
                || config.CutleryCharge > 0 
                || config.GiftWrapCharge > 0 
                || config.LowCartFeeCharge > 0 
                || config.LowCartAmountValue > 0;
        }

        #endregion

        #region DELETE (Optional but Included)

        public async Task<DeleteResponseRoot> DeleteAsync()
        {
            var dm = await _apiDbContext.Settings
                .FirstOrDefaultAsync();

            if (dm == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    "Settings not found");

            _apiDbContext.Settings.Remove(dm);

            await _apiDbContext.SaveChangesAsync();

            return new DeleteResponseRoot(true, "Settings deleted successfully");
        }

        #endregion
    }
}
