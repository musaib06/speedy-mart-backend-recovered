using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Siffrum.Ecom.BAL.LoginUsers;
using Siffrum.Ecom.BAL.Product;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Product.ProductControllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsProcess _settingsProcess;

        public SettingsController(SettingsProcess settingsProcess)
        {
            _settingsProcess = settingsProcess;
        }

        #region CREATE

        [HttpPost]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<SettingsSM>>> CreateSettings(
            [FromBody] ApiRequest<SettingsSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            var response = await _settingsProcess.CreateAsync(innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion

        #region READ

        [HttpGet]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, User, Seller")]
        public async Task<ActionResult<ApiResponse<SettingsSM>>> GetSettings()
        {
            var response = await _settingsProcess.GetAsync();
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion

        #region UPDATE

        [HttpPut]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<SettingsSM>>> UpdateSettings(
            [FromBody] ApiRequest<SettingsSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            var response = await _settingsProcess.UpdateAsync(innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion

        #region MAINTENANCE (Public — No Auth)

        [HttpGet("maintenance")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<MaintenanceStatusSM>>> GetMaintenanceStatus()
        {
            var settings = await _settingsProcess.GetAsync();
            var now = DateTime.UtcNow;
            var isActive = settings.IsMaintenanceMode
                           && settings.MaintenanceStartUtc.HasValue
                           && settings.MaintenanceEndUtc.HasValue
                           && now >= settings.MaintenanceStartUtc.Value
                           && now <= settings.MaintenanceEndUtc.Value;

            var result = new MaintenanceStatusSM
            {
                IsUnderMaintenance = isActive,
                MaintenanceStartUtc = isActive ? settings.MaintenanceStartUtc : null,
                MaintenanceEndUtc = isActive ? settings.MaintenanceEndUtc : null,
                Message = isActive
                    ? (settings.MaintenanceMessage ?? "We are under maintenance. Please try again later.")
                    : null
            };

            return ModelConverter.FormNewSuccessResponse(result);
        }

        #endregion

        #region DELETE

        [HttpDelete]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> DeleteSettings()
        {
            var response = await _settingsProcess.DeleteAsync();
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion
    }
}