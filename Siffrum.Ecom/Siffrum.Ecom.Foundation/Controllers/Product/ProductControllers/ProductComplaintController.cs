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
    [Route("api/v1/ProductComplaint")]
    public class ProductComplaintController : ApiControllerRoot
    {
        private readonly ProductComplaintProcess _process;

        public ProductComplaintController(ProductComplaintProcess process)
        {
            _process = process;
        }

        #region GET ALL (Admin)

        [HttpGet]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ProductComplaintSM>>>> GetAll(
            int? status = null, int? complaintType = null,
            int skip = 0, int top = 50)
        {
            var result = await _process.GetAllAsync(status, complaintType, skip, top);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region GET BY SELLER

        [HttpGet("seller/{sellerId}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<ProductComplaintSM>>>> GetBySeller(
            long sellerId, int? status = null,
            int skip = 0, int top = 50)
        {
            var result = await _process.GetBySellerAsync(sellerId, status, skip, top);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region GET BY ID

        [HttpGet("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<ProductComplaintSM>>> GetById(long id)
        {
            var result = await _process.GetByIdAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region CREATE (Seller)

        [HttpPost]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<ProductComplaintSM>>> Create(
            [FromBody] ApiRequest<ProductComplaintSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.CreateAsync(innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region UPDATE STATUS (Admin)

        [HttpPut("{id}/status")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> UpdateStatus(
            long id, int newStatus, string? resolutionNotes = null)
        {
            var result = await _process.UpdateStatusAsync(id, newStatus, resolutionNotes);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region ASSIGN (Admin)

        [HttpPut("{id}/assign/{adminId}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Assign(long id, long adminId)
        {
            var result = await _process.AssignAsync(id, adminId);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region ADD COMMENT

        [HttpPost("{id}/comment")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<ProductComplaintCommentSM>>> AddComment(
            long id,
            [FromBody] ApiRequest<ProductComplaintCommentSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));

            var result = await _process.AddCommentAsync(id, innerReq);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion

        #region DELETE

        [HttpDelete("{id}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long id)
        {
            var result = await _process.DeleteAsync(id);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        #endregion
    }
}
