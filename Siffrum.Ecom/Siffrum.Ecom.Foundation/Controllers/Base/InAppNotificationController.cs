using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Base
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema)]
    public class InAppNotificationController : ControllerBase
    {
        private readonly InAppNotificationProcess _process;

        public InAppNotificationController(InAppNotificationProcess process)
        {
            _process = process;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<InAppNotificationSM>>>> GetNotifications(int skip = 0, int top = 20)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            var recipientType = GetRecipientType();

            var notifications = await _process.GetNotifications(recipientType, userId, skip, top);
            return new ApiResponse<List<InAppNotificationSM>> { SuccessData = notifications };
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<UnreadCountSM>>> GetUnreadCount()
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            var recipientType = GetRecipientType();

            var count = await _process.GetUnreadCount(recipientType, userId);
            return new ApiResponse<UnreadCountSM> { SuccessData = new UnreadCountSM { Count = count } };
        }

        [HttpPost("{id}/read")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> MarkAsRead(long id)
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            var recipientType = GetRecipientType();

            await _process.MarkAsRead(id, recipientType, userId);
            return new ApiResponse<BoolResponseRoot> { SuccessData = new BoolResponseRoot(true, "Marked as read") };
        }

        [HttpPost("mark-all-read")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> MarkAllAsRead()
        {
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            var recipientType = GetRecipientType();

            await _process.MarkAllAsRead(recipientType, userId);
            return new ApiResponse<BoolResponseRoot> { SuccessData = new BoolResponseRoot(true, "All marked as read") };
        }

        private int GetRecipientType()
        {
            if (User.IsInRole("SuperAdmin") || User.IsInRole("SystemAdmin")) return 1;
            if (User.IsInRole("Seller")) return 3;
            if (User.IsInRole("DeliveryBoy")) return 5;
            return 4;
        }
    }
}
