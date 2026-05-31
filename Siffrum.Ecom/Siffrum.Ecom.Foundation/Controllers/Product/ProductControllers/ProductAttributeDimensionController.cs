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
    [Route("api/v1/Product/{productId}/attribute-dimensions")]
    public class ProductAttributeDimensionController : ApiControllerRoot
    {
        private readonly ProductAttributeDimensionProcess _process;

        public ProductAttributeDimensionController(ProductAttributeDimensionProcess process)
        {
            _process = process;
        }

        [HttpGet]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<ProductAttributeDimensionSM>>>> GetByProduct(long productId)
        {
            var result = await _process.GetByProductAsync(productId);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPost("bulk")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<ProductAttributeDimensionSM>>>> BulkSave(
            long productId,
            [FromBody] ApiRequest<List<ProductAttributeDimensionSM>> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.BulkSaveAsync(productId, innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpDelete("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long productId, long id)
        {
            var result = await _process.DeleteAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }
    }
}
