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
    [Route("api/v1/Wishlist")]
    [Authorize(
        AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
        Roles = "User")]
    public class WishlistController : ApiControllerRoot
    {
        private readonly WishlistProcess _process;

        public WishlistController(WishlistProcess process)
        {
            _process = process;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WishlistItemSM>>>> GetMyWishlist(int skip = 0, int top = 50, int? deliverySpeedType = null)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
                return NotFound(ModelConverter.FormNewErrorResponse(
                    DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));

            var result = await _process.GetWishlistAsync(userId, skip, top, deliverySpeedType);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetMyWishlistCount(int? deliverySpeedType = null)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
                return NotFound(ModelConverter.FormNewErrorResponse(
                    DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));

            var result = await _process.GetWishlistCountAsync(userId, deliverySpeedType);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpPost("{productVariantId}")]
        public async Task<ActionResult<ApiResponse<WishlistItemSM>>> AddToWishlist(long productVariantId)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
                return NotFound(ModelConverter.FormNewErrorResponse(
                    DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));

            var result = await _process.AddToWishlistAsync(userId, productVariantId);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpDelete("{productVariantId}")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> RemoveFromWishlist(long productVariantId)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
                return NotFound(ModelConverter.FormNewErrorResponse(
                    DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));

            var result = await _process.RemoveFromWishlistAsync(userId, productVariantId);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        [HttpGet("check/{productVariantId}")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> IsInWishlist(long productVariantId)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
                return NotFound(ModelConverter.FormNewErrorResponse(
                    DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));

            var result = await _process.IsInWishlistAsync(userId, productVariantId);
            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }
    }
}
