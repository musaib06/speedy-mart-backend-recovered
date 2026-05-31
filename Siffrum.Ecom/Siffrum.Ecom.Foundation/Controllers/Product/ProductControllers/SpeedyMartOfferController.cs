using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.BAL.Product;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Product.ProductControllers
{
    [ApiController]
    [Route("api/v1/SpeedyMart/offers")]
    public class SpeedyMartOfferController : ApiControllerRoot
    {
        private readonly SpeedyMartOfferProcess _process;

        public SpeedyMartOfferController(SpeedyMartOfferProcess process)
        {
            _process = process;
        }

        /// <summary>
        /// Admin: get all offers (active + inactive).
        /// </summary>
        [HttpGet]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<SpeedyMartOfferSM>>>> GetAll(
            DeliverySpeedTypeDM? deliverySpeed = null,
            bool? activeOnly = true,
            int skip = 0, int top = 50)
        {
            var result = await _process.GetAllAsync(deliverySpeed, activeOnly, skip, top);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        /// <summary>
        /// Public: get active offers only. No auth required — used by the mobile app.
        /// </summary>
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<SpeedyMartOfferSM>>>> GetActive(
            DeliverySpeedTypeDM? deliverySpeed = null,
            int skip = 0, int top = 50)
        {
            var result = await _process.GetAllAsync(deliverySpeed, true, skip, top);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<SpeedyMartOfferSM>>> GetById(long id)
        {
            var result = await _process.GetByIdAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPost]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<SpeedyMartOfferSM>>> Create(
            [FromBody] ApiRequest<SpeedyMartOfferSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.CreateAsync(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPut("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<SpeedyMartOfferSM>>> Update(
            long id,
            [FromBody] ApiRequest<SpeedyMartOfferSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.UpdateAsync(id, innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPut("{id}/toggle")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Toggle(long id)
        {
            var result = await _process.ToggleActiveAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpDelete("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long id)
        {
            var result = await _process.DeleteAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }
    }
}
