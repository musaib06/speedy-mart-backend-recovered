using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.BAL.Product;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Product.Category
{
    [ApiController]
    [Route("api/v1/Category/{categoryId}/attr-dimensions")]
    public class CategoryAttrDimensionController : ApiControllerRoot
    {
        private readonly CategoryAttrDimensionProcess _process;

        public CategoryAttrDimensionController(CategoryAttrDimensionProcess process)
        {
            _process = process;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<CategoryAttrDimensionSM>>>> GetByCategory(long categoryId)
        {
            var result = await _process.GetByCategory(categoryId);
            return ModelConverter.FormNewSuccessResponse(result);
        }

        [HttpPost("bulk")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<CategoryAttrDimensionSM>>>> BulkSave(
            long categoryId, [FromBody] ApiRequest<List<CategoryAttrDimensionSM>> apiRequest)
        {
            var items = apiRequest?.ReqData ?? new List<CategoryAttrDimensionSM>();
            var result = await _process.BulkSave(categoryId, items);
            return ModelConverter.FormNewSuccessResponse(result);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<CategoryAttrDimensionSM>>> Add(
            long categoryId, [FromBody] ApiRequest<CategoryAttrDimensionSM> apiRequest)
        {
            var req = apiRequest?.ReqData;
            if (req == null) return BadRequest();
            req.CategoryId = categoryId;
            var result = await _process.Add(req);
            return ModelConverter.FormNewSuccessResponse(result);
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<CategoryAttrDimensionSM>>> Update(
            long categoryId, long id, [FromBody] ApiRequest<CategoryAttrDimensionSM> apiRequest)
        {
            var req = apiRequest?.ReqData;
            if (req == null) return BadRequest();
            req.Id = id;
            req.CategoryId = categoryId;
            var result = await _process.Update(req);
            return ModelConverter.FormNewSuccessResponse(result);
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long categoryId, long id)
        {
            var result = await _process.Delete(id);
            return ModelConverter.FormNewSuccessResponse(result);
        }
    }
}
