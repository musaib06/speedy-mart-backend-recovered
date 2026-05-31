using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.ServiceModels.AppUser.Login;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.Foundation.Token;

namespace Siffrum.Ecom.BAL.Token
{
    public partial class TokenProcess : SiffrumBalBase
    {
        #region Properties

        private readonly IPasswordEncryptHelper _passwordEncryptHelper;
        private readonly ImageProcess _imageProcess;

        #endregion Properties

        #region Constructor
        public TokenProcess(IMapper mapper, ApiDbContext context, IPasswordEncryptHelper passwordEncryptHelper, ImageProcess imageProcess)
            : base(mapper, context)
        {
            _passwordEncryptHelper = passwordEncryptHelper;
            _imageProcess = imageProcess;
        }


        #endregion Constructor

        #region Token
        public async Task<(TokenUserSM, long)> ValidateLoginAndGenerateToken(TokenRequestSM tokenReq)
        {
            TokenUserSM? loginUserSM = null;
            long adminId = default;
            // add hash
            var passwordHash = await _passwordEncryptHelper.ProtectAsync<string>(tokenReq.Password);
            switch (tokenReq.RoleType)
            {
                case RoleTypeSM.SuperAdmin:
                case RoleTypeSM.SystemAdmin:                
                    var appUser = await _apiDbContext.Admin
                        .FirstOrDefaultAsync(x => (x.Username.ToLower() == tokenReq.LoginId.ToLower() || x.Email.ToLower() == tokenReq.LoginId.ToLower()) && x.Password == passwordHash && x.RoleType == (RoleTypeDM)tokenReq.RoleType);
                    if (appUser != null)
                    {
                        if(appUser.Status == StatusDM.Inactive)
                        {
                            throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                $"Admin with Username/Email: {tokenReq.LoginId} is inactive, but tries to login", "Invalid Credentials");
                        }
                        loginUserSM = _mapper.Map<TokenUserSM>(appUser);
                    }

                    break;
                case RoleTypeSM.User:
                    {
                        var endUser = await _apiDbContext.User
                        .Where(u => (u.Email.ToLower() == tokenReq.LoginId.ToLower() || u.Username.ToLower() == tokenReq.LoginId.ToLower()))
                        .FirstOrDefaultAsync();
                        if (endUser != null)
                        {
                            if (string.IsNullOrEmpty(endUser.Password))
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please log in using your Google or Facebook account or Click on Forgot Password to change your password", "Please log in using your Google or Facebook account or Click on Forgot Password to change your password");
                            }

                            if (endUser.Status == StatusDM.Inactive)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"User with Username/Email: {tokenReq.LoginId} is inactive, but tries to login", "Invalid Credentials");
                            }

                            // Todo: Decide whether todo password less login or not

                        }                      
                        
                        var data = await (from user in _apiDbContext.User
                                          where (user.Email.ToLower() == tokenReq.LoginId.ToLower() || user.Username.ToLower() == tokenReq.LoginId.ToLower()) && user.Password == passwordHash
                                          select new { User = user }).FirstOrDefaultAsync();

                        if (data != null && data.User != null)
                        {
                            if (data.User.Status == StatusDM.Inactive)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"User with Username/Email: {tokenReq.LoginId} is inactive, but tries to login", "Invalid Credentials");
                            }
                            loginUserSM = _mapper.Map<TokenUserSM>(data.User);
                        }
                    }
                    break;
                case RoleTypeSM.DeliveryBoy:
                    {
                        var endUser = await _apiDbContext.DeliveryBoy
                        .Where(u => (u.Email.ToLower() == tokenReq.LoginId.ToLower() || u.Username.ToLower() == tokenReq.LoginId.ToLower()))
                        .FirstOrDefaultAsync();
                        if (endUser != null)
                        {
                            if (string.IsNullOrEmpty(endUser.Password))
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please log in using your Google or Facebook account or Click on Forgot Password to change your password", "Please log in using your Google or Facebook account or Click on Forgot Password to change your password");
                            }

                            // Todo: Decide whether todo password less login or not

                        }
                        
                        var data = await (from user in _apiDbContext.DeliveryBoy
                                          where (user.Email.ToLower() == tokenReq.LoginId.ToLower() || user.Username.ToLower() == tokenReq.LoginId.ToLower()) && user.Password == passwordHash
                                          select new { User = user, AdminId = user.AdminId }).FirstOrDefaultAsync();

                        if (data != null && data.User != null)
                        {
                            if (data.User.Status == DeliveryBoyStatusDM.Deactivated || data.User.Status == DeliveryBoyStatusDM.Rejected || data.User.Status == DeliveryBoyStatusDM.Removed)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"Delivery boy with Username/Email: {tokenReq.LoginId} is deactivated/Rejected or Removed, but tries to login", "Invalid Credentials");
                            }
                            loginUserSM = _mapper.Map<TokenUserSM>(data.User);
                        }
                    }
                    break;

                case RoleTypeSM.Seller:
                    {
                        var endUser = await _apiDbContext.Seller
                        .Where(u => (u.Email.ToLower() == tokenReq.LoginId.ToLower() || u.Username.ToLower() == tokenReq.LoginId.ToLower()))
                        .FirstOrDefaultAsync();
                        if (endUser != null)
                        {
                            if(endUser.Status == SellerStatusDM.Removed)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"Seller with Username/Email: {tokenReq.LoginId} is Removed, but tries to login", "Your account has been removed. Please contact the admin.");
                            }
                            if(endUser.Status == SellerStatusDM.Deactivated)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"Seller with Username/Email: {tokenReq.LoginId} is Deactivated, but tries to login", "Your account has been deactivated. Please contact the admin.");
                            }
                            if(endUser.Status == SellerStatusDM.Rejected)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"Seller with Username/Email: {tokenReq.LoginId} is Rejected, but tries to login", "Your account has been rejected. Please contact the admin.");
                            }
                            if(endUser.Status == SellerStatusDM.Blocked)
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidToken_Log,
                                    $"Seller with Username/Email: {tokenReq.LoginId} is Blocked, but tries to login", "Your account has been blocked due to policy violations. Please contact the admin.");
                            }
                            if (string.IsNullOrEmpty(endUser.Password))
                            {
                                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, @"Please log in using your Google or Facebook account or Click on Forgot Password to change your password", "Please log in using your Google or Facebook account or Click on Forgot Password to change your password");
                            }

                            // Todo: Decide whether todo password less login or not

                        }

                        var data = await (from user in _apiDbContext.Seller
                                          where (user.Email.ToLower() == tokenReq.LoginId.ToLower() || user.Username.ToLower() == tokenReq.LoginId.ToLower()) && user.Password == passwordHash
                                          select new { User = user }).FirstOrDefaultAsync();

                        if (data != null && data.User != null)
                        {
                            loginUserSM = _mapper.Map<TokenUserSM>(data.User);
                            loginUserSM.Status = StatusSM.Active;
                            if (data.User.AdminId.HasValue)
                            {
                                adminId = (long)data.User.AdminId;
                            }
                        }
                    }
                    break;
                    
            }
            if (loginUserSM != null)
            {                
                if (!string.IsNullOrEmpty(loginUserSM.Image))
                {
                    var tImg = await _imageProcess.ResolveImage(loginUserSM.Image);
                    loginUserSM.Image = tImg.Base64;
                    loginUserSM.NetworkImage = tImg.NetworkUrl;
                }
            }
            return (loginUserSM, adminId);


        }

        #endregion Token

        #region Security Stamp

        /// <summary>
        /// Generates a new SecurityStamp for the given user/delivery boy and saves to DB.
        /// Returns the new stamp to be embedded in the JWT.
        /// </summary>
        public async Task<string> RotateSecurityStampAsync(long userId, RoleTypeSM roleType)
        {
            var newStamp = Guid.NewGuid().ToString();

            switch (roleType)
            {
                case RoleTypeSM.User:
                    var user = await _apiDbContext.User.FindAsync(userId);
                    if (user != null)
                    {
                        user.SecurityStamp = newStamp;
                        await _apiDbContext.SaveChangesAsync();
                    }
                    break;

                case RoleTypeSM.DeliveryBoy:
                    var deliveryBoy = await _apiDbContext.DeliveryBoy.FindAsync(userId);
                    if (deliveryBoy != null)
                    {
                        deliveryBoy.SecurityStamp = newStamp;
                        await _apiDbContext.SaveChangesAsync();
                    }
                    break;
            }

            return newStamp;
        }

        /// <summary>
        /// Returns the current SecurityStamp from DB for the given user/delivery boy.
        /// Returns null if user not found or stamp not set.
        /// </summary>
        public async Task<string?> GetSecurityStampAsync(long userId, RoleTypeSM roleType)
        {
            switch (roleType)
            {
                case RoleTypeSM.User:
                    return await _apiDbContext.User
                        .Where(u => u.Id == userId)
                        .Select(u => u.SecurityStamp)
                        .FirstOrDefaultAsync();

                case RoleTypeSM.DeliveryBoy:
                    return await _apiDbContext.DeliveryBoy
                        .Where(d => d.Id == userId)
                        .Select(d => d.SecurityStamp)
                        .FirstOrDefaultAsync();

                default:
                    return null;
            }
        }

        #endregion Security Stamp

        #region Private Methods

        #endregion Private Methods
    }
}
