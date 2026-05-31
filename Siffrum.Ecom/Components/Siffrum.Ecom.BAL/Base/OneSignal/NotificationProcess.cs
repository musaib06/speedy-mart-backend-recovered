using AutoMapper;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.Config.Configuration;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.ServiceModels.AppUser.Login;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;
using System.Text;
using System.Text.Json;

namespace Siffrum.Ecom.BAL.Base.OneSignal
{
    public class NotificationProcess : SiffrumBalBase
    {
        private readonly APIConfiguration _apiConfiguration;
        private readonly IHttpClientFactory _httpClientFactory;

        public NotificationProcess(
            IMapper mapper,
            ApiDbContext context,
            APIConfiguration apiConfiguration,
            IHttpClientFactory httpClientFactory)
            : base(mapper, context)
        {
            _apiConfiguration = apiConfiguration;
            _httpClientFactory = httpClientFactory;
        }


        #region Send Push To Single User

        public async Task<BoolResponseRoot> SendPushNotification(SendNotificationMessageSM request)
        {
            var token = await _apiDbContext.User
                .Where(x => x.Id == request.UserIds[0] &&
                            !string.IsNullOrEmpty(x.FcmId) &&
                            x.Status == StatusDM.Active)
                .Select(x => x.FcmId!)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(token))
                return new BoolResponseRoot(false, "User device not found or token is missing");

            return await SendFirebasePush(new[] { token }, request);
        }

        public async Task<BoolResponseRoot> SendPushNotificationByPlayerId(string playerId, SendNotificationMessageSM request)
        {
            if (string.IsNullOrEmpty(playerId))
                return new BoolResponseRoot(false, "User device not found or token is missing");

            return await SendFirebasePush(new[] { playerId }, request);
        }

        public async Task<BoolResponseRoot> SendPushNotificationToUser(SendNotificationMessageSM request, string fcmId)
        {
            if (string.IsNullOrEmpty(fcmId))
                return new BoolResponseRoot(false, "No valid device token");

            return await SendFirebasePush(new[] { fcmId }, request);
        }

        #endregion

        #region Send Push To Multiple Users

        public async Task<BoolResponseRoot> SendBulkPushNotification(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.User
                .Where(x => request.UserIds.Contains(x.Id) &&
                            !string.IsNullOrEmpty(x.FcmId) &&
                            x.Status == StatusDM.Active)
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No valid device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendBulkPushNotificationToAdmins(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.Admin
                .Where(x => !string.IsNullOrEmpty(x.FcmId) &&
                            x.Status == StatusDM.Active)
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No valid device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendBulkPushNotificationToSellerForOrder(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.Seller
                .Where(x => request.UserIds.Contains(x.Id) &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No valid device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendBulkPushNotificationToDeliveryBoysForOrder(SendNotificationMessageSM request, string pincode)
        {
            var ids = await _apiDbContext.DeliveryBoyPincodes
                .Where(x => pincode.Contains(x.Pincode))
                .Select(x => x.DeliveryBoyId)
                .Distinct()
                .ToListAsync();
            var tokens = await _apiDbContext.DeliveryBoy
                .Where(x => ids.Contains(x.Id) &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No valid device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendPushNotificationToSellerDeliveryBoys(SendNotificationMessageSM request, long sellerId)
        {
            var totalBoys = await _apiDbContext.DeliveryBoy
                .Where(x => x.SellerId == sellerId)
                .CountAsync();
            var activeBoys = await _apiDbContext.DeliveryBoy
                .Where(x => x.SellerId == sellerId && x.Status == DeliveryBoyStatusDM.Active)
                .CountAsync();
            var availableBoys = await _apiDbContext.DeliveryBoy
                .Where(x => x.SellerId == sellerId && x.Status == DeliveryBoyStatusDM.Active && x.IsAvailable == 1)
                .CountAsync();

            var tokens = await _apiDbContext.DeliveryBoy
                .Where(x => x.SellerId == sellerId &&
                            x.Status == DeliveryBoyStatusDM.Active &&
                            x.IsAvailable == 1 &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            Console.WriteLine($"[FCM-DBOY] Seller {sellerId}: total={totalBoys}, active={activeBoys}, available(online)={availableBoys}, withToken={tokens.Count}");
            if (tokens.Any())
                Console.WriteLine($"[FCM-DBOY] Tokens: {string.Join(", ", tokens.Select(s => s.Length > 12 ? s[..6] + "..." + s[^6..] : s))}");

            if (!tokens.Any())
                return new BoolResponseRoot(false, $"No delivery boys with push token for seller {sellerId} (total={totalBoys}, active={activeBoys}, online={availableBoys})");

            return await SendFirebasePush(tokens, request);
        }

        #endregion

        #region Broadcast Push Notification

        public async Task<BoolResponseRoot> BroadcastPushNotification(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.User
                .Where(x => x.Status == StatusDM.Active &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No valid device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendPushToAllSellers(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.Seller
                .Where(x => !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No seller device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendPushToSelectedSellers(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.Seller
                .Where(x => request.UserIds.Contains(x.Id) &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No seller device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendPushToUsersOfSellers(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.User
                .Where(x => x.AssignedSellerId.HasValue &&
                            request.UserIds.Contains(x.AssignedSellerId.Value) &&
                            x.Status == StatusDM.Active &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No user device tokens found for the selected sellers");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendPushToAllUsers(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.User
                .Where(x => x.Status == StatusDM.Active &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No user device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        public async Task<BoolResponseRoot> SendPushToSelectedUsers(SendNotificationMessageSM request)
        {
            var tokens = await _apiDbContext.User
                .Where(x => request.UserIds.Contains(x.Id) &&
                            x.Status == StatusDM.Active &&
                            !string.IsNullOrEmpty(x.FcmId))
                .Select(x => x.FcmId!)
                .ToListAsync();

            if (!tokens.Any())
                return new BoolResponseRoot(false, "No user device tokens found");

            return await SendFirebasePush(tokens, request);
        }

        #endregion

        #region Send OTP SMS

        public async Task<BoolResponseRoot> SendOtpSms(string phoneNumber, int otp)
        {
            try
            {
                var smsSettings = _apiConfiguration.SmsSettings;
                var client = _httpClientFactory.CreateClient();

                var url = $"{smsSettings.BaseUrl}/SendSMS";
                var otpMessage = $"{otp} is your otp to login to SpeedyKart. SpeedyKart never calls to ask for OTP. The OTP expires in 2 mins.-Speedykart";
                var payload = new
                {
                    userid = smsSettings.UserId,
                    pwd = smsSettings.Password,
                    mobile = phoneNumber,
                    sender = smsSettings.Sender,
                    msg = otpMessage,
                    msgtype = smsSettings.MsgType,
                    peid = smsSettings.PeId,
                    templateid = smsSettings.TemplateId
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                Console.WriteLine($"[SMS] Sending OTP to {phoneNumber}, payload: {JsonSerializer.Serialize(payload)}");

                var response = await client.SendAsync(request);

                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[SMS] Response status: {response.StatusCode}, body: {responseBody}");

                if (!response.IsSuccessStatusCode)
                    return new BoolResponseRoot(false, responseBody);

                // Deserialize response
                var smsResponse = JsonSerializer.Deserialize<List<SmsResponseSM>>(responseBody);

                var message = smsResponse?.FirstOrDefault()?.Response;

                Console.WriteLine($"[SMS] Parsed response message: {message}");

                string messageId = "";

                if (!string.IsNullOrEmpty(message) && message.Contains("Message ID:"))
                {
                    messageId = message.Split("Message ID:").Last().Trim();
                }

                return new BoolResponseRoot(true, messageId);
            }
            catch (Exception ex)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    ex.Message,
                    "Failed to send OTP SMS");
            }
        }

        public async Task<BoolResponseRoot> GetSmsDeliveryStatus(string messageId)
        {
            try
            {
                var smsSettings = _apiConfiguration.SmsSettings;
                var client = _httpClientFactory.CreateClient();

                var url = $"{smsSettings.BaseUrl}/GetDelivery";

                var payload = new
                {
                    userId = smsSettings.UserId,
                    pwd = smsSettings.Password,
                    msgId = messageId
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return new BoolResponseRoot(false, responseBody);

                var smsResponse = JsonSerializer.Deserialize<List<SmsResponseSM>>(responseBody);

                var message = smsResponse?.FirstOrDefault()?.Response ?? "";

                if (message.Contains("Delivery Status : Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    return new BoolResponseRoot(true, message);
                }

                return new BoolResponseRoot(false, message);
            }
            catch (Exception ex)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    ex.Message,
                    "Failed to fetch SMS delivery status");
            }
        }

        #endregion

        #region Firebase FCM Core

        private async Task<BoolResponseRoot> SendFirebasePush(IList<string> tokens, SendNotificationMessageSM request)
        {
            try
            {
                var data = request.AdditionalData != null && request.AdditionalData.Count > 0
                    ? request.AdditionalData.ToDictionary(k => k.Key, v => v.Value)
                    : new Dictionary<string, string>();

                // Always include title/message in data payload for apps that handle data-only messages
                data["title"] = request.Title ?? "";
                data["body"] = request.Message ?? "";

                int success = 0, failure = 0;

                // Firebase allows max 500 tokens per multicast
                foreach (var batch in tokens.Chunk(500))
                {
                    var message = new MulticastMessage
                    {
                        Tokens = batch.ToList(),
                        Notification = new Notification
                        {
                            Title = request.Title,
                            Body = request.Message
                        },
                        Data = data,
                        Android = new AndroidConfig
                        {
                            Priority = Priority.High,
                            Notification = new AndroidNotification
                            {
                                Sound = "default",
                                ChannelId = "default_channel"
                            }
                        },
                        Apns = new ApnsConfig
                        {
                            Aps = new Aps
                            {
                                Sound = "default",
                                ContentAvailable = true
                            }
                        }
                    };

                    Console.WriteLine($"[FCM] Sending to {batch.Length} token(s): {request.Title}");
                    Console.WriteLine($"[FCM] Token(s): {string.Join(", ", batch.Select(t => t.Length > 20 ? t[..10] + "..." + t[^10..] : t))}");
                    var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                    success += response.SuccessCount;
                    failure += response.FailureCount;
                    Console.WriteLine($"[FCM] Result: success={response.SuccessCount}, failure={response.FailureCount}");
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        var r = response.Responses[i];
                        if (!r.IsSuccess)
                            Console.WriteLine($"[FCM] FAILED token[{i}]: {r.Exception?.MessagingErrorCode} - {r.Exception?.Message}");
                    }
                }

                if (success == 0 && failure > 0)
                    return new BoolResponseRoot(false, $"All {failure} FCM message(s) failed");

                return new BoolResponseRoot(true, $"FCM sent: success={success}, failure={failure}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM] Exception: {ex.Message}");
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    $"Message: {ex.Message}, InnerException: {ex?.InnerException}, StackTrace: {ex?.StackTrace}",
                    "Something went wrong while sending notification, Please try again later");
            }
        }

        #endregion

        #region Generate OTP

        public int GenerateOtp()
        {
            return Random.Shared.Next(100000, 999999);
        }

        #endregion
    }
}