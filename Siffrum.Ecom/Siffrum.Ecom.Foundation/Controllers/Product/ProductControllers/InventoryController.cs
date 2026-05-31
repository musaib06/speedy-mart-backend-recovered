using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.BAL.Product;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Product.ProductControllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class InventoryController : ApiControllerRoot
    {
        private readonly InventoryTransactionProcess _inventoryTx;

        public InventoryController(InventoryTransactionProcess inventoryTx)
        {
            _inventoryTx = inventoryTx;
        }

        [HttpGet("transactions")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<InventoryTransactionSM>>>> GetTransactions(
            long? sellerId = null,
            long? variantId = null,
            string? changeType = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            int skip = 0,
            int top = 50)
        {
            var response = await _inventoryTx.GetAdminTransactionsAsync(
                sellerId, variantId, changeType, dateFrom, dateTo, skip, top);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("transactions/count")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetTransactionsCount(
            long? sellerId = null,
            long? variantId = null,
            string? changeType = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var response = await _inventoryTx.GetAdminTransactionsCountAsync(
                sellerId, variantId, changeType, dateFrom, dateTo);
            return ModelConverter.FormNewSuccessResponse(response);
        }
    }
}
