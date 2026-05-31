using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.BAL.Base.OneSignal;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.AppUser.Login;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.Foundation.Controllers.Product.ProductControllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [AllowAnonymous]
    public class NotificationController : ApiControllerRoot
    {
        private readonly NotificationProcess _notificationProcess;
        private readonly InAppNotificationProcess _inAppNotificationProcess;

        public NotificationController(NotificationProcess process, InAppNotificationProcess inAppNotificationProcess)
        {
            _notificationProcess = process;
            _inAppNotificationProcess = inAppNotificationProcess;
        }

        #region Add

        [HttpPost("bulk")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SendBulkNotification(
            [FromBody] ApiRequest<SendNotificationMessageSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion

            var response = await _notificationProcess.SendBulkPushNotification(innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }
        
        [HttpPost("single")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SendSingleNotification(
            [FromBody] ApiRequest<SendNotificationMessageSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion

            var response = await _notificationProcess.SendPushNotification(innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPost("single/{playerId}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SendSingleNotificationByPlayerId(
            string playerId,
            [FromBody] ApiRequest<SendNotificationMessageSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion

            var response = await _notificationProcess.SendPushNotificationByPlayerId(playerId,innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }
        
        [HttpPost("broadcast")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Broadcast(            
            [FromBody] ApiRequest<SendNotificationMessageSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion

            var response = await _notificationProcess.BroadcastPushNotification(innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }
        
        [HttpPost("broadcast-message")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BroadcastResultSM>>> BroadcastMessage(
            [FromBody] ApiRequest<BroadcastMessageSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null || string.IsNullOrWhiteSpace(innerReq.Title) || string.IsNullOrWhiteSpace(innerReq.Message))
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    "Title and Message are required", ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            var result = new BroadcastResultSM { Success = true };
            var pushMsg = new SendNotificationMessageSM
            {
                Title = innerReq.Title,
                Message = innerReq.Message,
                UserIds = innerReq.RecipientIds ?? new List<long>(),
                AdditionalData = new Dictionary<string, string>
                {
                    { "type", "broadcast" }
                }
            };

            try
            {
                switch (innerReq.TargetType)
                {
                    case "AllSellers":
                        await _notificationProcess.SendPushToAllSellers(pushMsg);
                        await _inAppNotificationProcess.NotifyAllSellers(
                            innerReq.Title, innerReq.Message, "broadcast");
                        result.InAppSent = 1;
                        result.PushSent = 1;
                        break;

                    case "SelectedSellers":
                        if (innerReq.RecipientIds == null || !innerReq.RecipientIds.Any())
                            return BadRequest(ModelConverter.FormNewErrorResponse(
                                "RecipientIds required for SelectedSellers", ApiErrorTypeSM.InvalidInputData_NoLog));
                        await _notificationProcess.SendPushToUsersOfSellers(pushMsg);
                        result.PushSent = 1;
                        break;

                    case "AllUsers":
                        await _notificationProcess.SendPushToAllUsers(pushMsg);
                        result.PushSent = 1;
                        break;

                    case "SelectedUsers":
                        if (innerReq.RecipientIds == null || !innerReq.RecipientIds.Any())
                            return BadRequest(ModelConverter.FormNewErrorResponse(
                                "RecipientIds required for SelectedUsers", ApiErrorTypeSM.InvalidInputData_NoLog));
                        await _notificationProcess.SendPushToSelectedUsers(pushMsg);
                        result.PushSent = 1;
                        break;

                    default:
                        return BadRequest(ModelConverter.FormNewErrorResponse(
                            "Invalid TargetType. Use: AllUsers, SelectedUsers, AllSellers, SelectedSellers",
                            ApiErrorTypeSM.InvalidInputData_NoLog));
                }

                result.TotalRecipients = innerReq.RecipientIds?.Count ?? 0;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return Ok(ModelConverter.FormNewSuccessResponse(result));
        }

        /*[HttpPost("sms")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SMS(            
            [FromBody] ApiRequest<SendNotificationMessageSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;

            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion

            var response = await _notificationProcess.SendOtpSms("+917006636038", 123456);
            return ModelConverter.FormNewSuccessResponse(response);
        }*/

        #endregion
    }
}