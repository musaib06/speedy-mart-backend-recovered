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
    [Route("api/v1/Category/{categoryId}/spec-templates")]
    public class CategorySpecTemplateController : ApiControllerRoot
    {
        private readonly CategorySpecTemplateProcess _process;

        public CategorySpecTemplateController(CategorySpecTemplateProcess process)
        {
            _process = process;
        }

        #region GET ALL FOR CATEGORY

        [HttpGet]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<CategorySpecTemplateSM>>>> GetByCategory(long categoryId)
        {
            var result = await _process.GetByCategoryAsync(categoryId);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region BULK SAVE (replaces all templates for a category)

        [HttpPost("bulk")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<CategorySpecTemplateSM>>>> BulkSave(
            long categoryId,
            [FromBody] ApiRequest<List<CategorySpecTemplateSM>> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.BulkSaveAsync(categoryId, innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region ADD SINGLE

        [HttpPost]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<CategorySpecTemplateSM>>> Add(
            long categoryId,
            [FromBody] ApiRequest<CategorySpecTemplateSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.AddAsync(categoryId, innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region UPDATE SINGLE

        [HttpPut("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<CategorySpecTemplateSM>>> Update(
            long categoryId,
            long id,
            [FromBody] ApiRequest<CategorySpecTemplateSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.UpdateAsync(id, innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region DELETE SINGLE

        [HttpDelete("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long categoryId, long id)
        {
            var result = await _process.DeleteAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion
    }
}
