using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.BAL.Product;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Product.ProductControllers
{
    [ApiController]
    [Route("api/v1/LowStockAlert")]
    public class LowStockAlertController : ApiControllerRoot
    {
        private readonly LowStockAlertProcess _process;

        public LowStockAlertController(LowStockAlertProcess process)
        {
            _process = process;
        }

        [HttpGet("seller/{sellerId}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<LowStockAlertSM>>>> GetBySeller(long sellerId, bool activeOnly = true)
        {
            var result = await _process.GetBySellerAsync(sellerId, activeOnly);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPost]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<LowStockAlertSM>>> Create(
            [FromBody] ApiRequest<LowStockAlertSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.CreateAsync(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPut("{id}/deactivate")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Deactivate(long id)
        {
            var result = await _process.DeactivateAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpDelete("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long id)
        {
            var result = await _process.DeleteAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }
    }
}
