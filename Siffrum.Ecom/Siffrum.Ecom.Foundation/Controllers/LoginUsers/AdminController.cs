using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.BAL.LoginUsers;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;
using Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard;
using Siffrum.Ecom.ServiceModels.v1.General;

namespace Siffrum.Ecom.Foundation.Controllers.LoginUsers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
    public class AdminController : ApiControllerWithOdataRoot<AdminSM>
    {
        private readonly AdminProcess _adminProcess;
        private readonly AdminDashboardProcess _adminDashboard;
        private readonly ActivityLogProcess _activityLogProcess;
        public AdminController(AdminProcess process, AdminDashboardProcess adminDashboard, ActivityLogProcess activityLogProcess)
            : base(process)
        { 
            _adminProcess = process;
            _adminDashboard = adminDashboard;
            _activityLogProcess = activityLogProcess;
        }

        [HttpGet]
        [Route("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]       
        public async Task<ActionResult<ApiResponse<IEnumerable<AdminSM>>>> GetAsOdata(ODataQueryOptions<AdminSM> oDataOptions)
        {
            //oDataOptions.Filter = new FilterQueryOption();
            //TODO: validate inputs here probably 
            //if (oDataOptions.Filter == null)
            //    oDataOptions.Filter. = "$filter=organisationUnitId%20eq%20" + 10 + ",";
            var retList = await GetAsEntitiesOdata(oDataOptions);

            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }

        [HttpGet("")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<AdminSM>>>> GetAllAdmins(int skip, int top)
        {
            #region Check Request           

            #endregion Check Request

            var response = await _adminProcess.GetAll(skip, top);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetAllAdminsCount()
        {
            #region Check Request           

            #endregion Check Request

            var response = await _adminProcess.GetAllAdminsCount();
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPost("register")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> RegisterAdmin([FromBody] ApiRequest<AdminSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request
            var addedSM = await _adminProcess.RegisterAdmin(innerReq);
            if (addedSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(addedSM);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPost("force-reset-password/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> ForceResetPassword(long id, [FromBody] ApiRequest<SetPasswordRequestSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null || string.IsNullOrWhiteSpace(innerReq.Password))
            {
                return BadRequest(ModelConverter.FormNewErrorResponse("New password is required", ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var response = await _adminProcess.ForceResetPassword(id, innerReq.Password);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPost("assign-devicetoken")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> AssignDeviceTokenToDeliveryBoy([FromBody] ApiRequest<DeviceTokenSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            var adminId = User.GetUserRecordIdFromCurrentUserClaims();
            if (adminId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }

            #endregion Check Request
            var addedSM = await _adminProcess.AssignDeviceTokenToDeliveryBoy(adminId, innerReq);
            if (addedSM != null)
            {
                return ModelConverter.FormNewSuccessResponse(addedSM);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> ForgotPassword([FromBody] ApiRequest<ForgotPasswordSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request

            var response = await _adminProcess.SendResetPasswordLink(innerReq);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> ResetPassword([FromBody] ApiRequest<ResetPasswordRequestSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }

            #endregion Check Request
            var response = await _adminProcess.UpdatePassword(innerReq);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPost("update-password")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> UpdatePassword([FromBody] ApiRequest<UpdatePasswordRequestSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if(userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request

            var response = await _adminProcess.ChangePassword(userId, innerReq);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpPost("set-password")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SetPassword([FromBody] ApiRequest<SetPasswordRequestSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request

            var response = await _adminProcess.SetPassord(userId, innerReq);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }


        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<AdminSM>>> UpdateAdmin(long id, [FromBody] ApiRequest<AdminSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var role = User.GetUserRoleTypeFromCurrentUserClaims();
            if (role != RoleTypeDM.SuperAdmin.ToString())
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Role_NotFound));
            }
            #endregion Check Request

            var response = await _adminProcess.UpdateAsync(id, innerReq);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        
        [HttpPut("mine")]
        public async Task<ActionResult<ApiResponse<AdminSM>>> UpdateMineAccount([FromBody] ApiRequest<AdminSM> apiRequest)
        {
            #region Check Request

            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if(userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request

            var response = await _adminProcess.UpdateAsync(userId, innerReq);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<AdminDashboardResponseSM>>> Dashboard(DateTime? date = null, int? platform = null)
        {

            var response = await _adminDashboard.GetDashboardAsync(date, platform);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        [HttpGet("seller-collections")]
        public async Task<ActionResult<ApiResponse<List<SellerCollectionSM>>>> GetSellerCollections(DateTime? from = null, DateTime? to = null)
        {
            var response = await _adminDashboard.GetSellerCollectionsAsync(from, to);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("order-drilldown/sellers")]
        public async Task<ActionResult<ApiResponse<List<SellerOrderSummarySM>>>> GetOrderDrillDownSellers(
            DateTime date, string status = "all")
        {
            var response = await _adminDashboard.GetSellerOrderSummaryAsync(date, status);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("order-drilldown/seller/{sellerId}")]
        public async Task<ActionResult<ApiResponse<List<OrderDrillDownItemSM>>>> GetOrderDrillDownDetails(
            long sellerId, DateTime date, string status = "all")
        {
            var response = await _adminDashboard.GetSellerOrderDetailsAsync(sellerId, date, status);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("mine")]
        public async Task<ActionResult<ApiResponse<AdminSM>>> GetSeller()
        {
            #region Check Request
            
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if(userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request
            var response = await _adminProcess.GetByIdAsync(userId);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AdminSM>>> GetById(long id)
        {
            #region Check Request    

            var role = User.GetUserRoleTypeFromCurrentUserClaims();
            if (role != RoleTypeDM.SuperAdmin.ToString())
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Role_NotFound));
            }

            #endregion Check Request

            var response = await _adminProcess.GetByIdAsync(id);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> DeleteAdmin(long id)
        {
            #region Check Request           
            var role = User.GetUserRoleTypeFromCurrentUserClaims();
            if (role != RoleTypeDM.SuperAdmin.ToString())
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Role_NotFound));
            }
            #endregion Check Request

            var response = await _adminProcess.DeleteAsync(id);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }
        [HttpDelete("mine")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> DeleteMineAccount()
        {
            #region Check Request           
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request

            var response = await _adminProcess.DeleteAsync(userId);
            if (response != null)
            {
                return ModelConverter.FormNewSuccessResponse(response);
            }
            else
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_PassedDataNotSaved, ApiErrorTypeSM.NoRecord_NoLog));
            }
        }

        #region Activity Logs

        /// <summary>
        /// Get activity logs with filtering and pagination (up to 5000 records)
        /// </summary>
        [HttpGet("activity-logs")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<GetActivityLogsResponseSM>>> GetActivityLogs(
            int skip = 0,
            int take = 50,
            string? userType = null,
            string? actionType = null,
            string? actionCategory = null,
            long? userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            // Limit take to max 5000 records as per requirement
            if (take > 5000) take = 5000;
            if (take < 1) take = 50;

            var request = new GetActivityLogsRequestSM
            {
                Skip = skip,
                Take = take,
                UserType = userType,
                ActionType = actionType,
                ActionCategory = actionCategory,
                UserId = userId,
                FromDate = fromDate,
                ToDate = toDate
            };

            var response = await _activityLogProcess.GetLogsAsync(request);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        /// <summary>
        /// Get activity logs summary for dashboard
        /// </summary>
        [HttpGet("activity-logs/summary")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<ActivityLogSummarySM>>> GetActivityLogsSummary()
        {
            var response = await _activityLogProcess.GetSummaryAsync();
            return ModelConverter.FormNewSuccessResponse(response);
        }

        /// <summary>
        /// Get filter options for activity logs
        /// </summary>
        [HttpGet("activity-logs/filters")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<Dictionary<string, List<string>>>>> GetActivityLogFilters()
        {
            var userTypes = await _activityLogProcess.GetUserTypesAsync();
            var actionTypes = await _activityLogProcess.GetActionTypesAsync();
            var actionCategories = await _activityLogProcess.GetActionCategoriesAsync();

            var filters = new Dictionary<string, List<string>>
            {
                { "userTypes", userTypes },
                { "actionTypes", actionTypes },
                { "actionCategories", actionCategories }
            };

            return ModelConverter.FormNewSuccessResponse(filters);
        }

        /// <summary>
        /// Get logs for a specific entity
        /// </summary>
        [HttpGet("activity-logs/entity/{entityType}/{entityId}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ActivityLogSM>>>> GetEntityActivityLogs(string entityType, long entityId, int take = 50)
        {
            var response = await _activityLogProcess.GetEntityLogsAsync(entityType, entityId, take);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        /// <summary>
        /// Get logs for a specific user
        /// </summary>
        [HttpGet("activity-logs/user/{userId}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ActivityLogSM>>>> GetUserActivityLogs(long userId, string? userType = null, int take = 100)
        {
            var response = await _activityLogProcess.GetUserLogsAsync(userId, userType, take);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion Activity Logs
    }
}
