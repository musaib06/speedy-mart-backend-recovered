using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.BAL.Base.OneSignal;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.ServiceModels.AppUser.Login;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class OrderComplaintProcess : SiffrumBalBase
    {
        private readonly NotificationProcess _notificationProcess;
        private readonly InAppNotificationProcess _inAppNotificationProcess;
        private readonly ImageProcess _imageProcess;
        private const int MaxUserMessages = 5;

        public OrderComplaintProcess(IMapper mapper, ApiDbContext apiDbContext, NotificationProcess notificationProcess,
            InAppNotificationProcess inAppNotificationProcess, ImageProcess imageProcess)
            : base(mapper, apiDbContext)
        {
            _notificationProcess = notificationProcess;
            _inAppNotificationProcess = inAppNotificationProcess;
            _imageProcess = imageProcess;
        }

        #region User - Submit Complaint

        public async Task<OrderComplaintSM> SubmitComplaint(long userId, OrderComplaintSM sm)
        {
            var order = await _apiDbContext.Order
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == sm.OrderId && o.UserId == userId);

            if (order == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found or does not belong to you");

            var dm = _mapper.Map<OrderComplaintDM>(sm);
            dm.UserId = userId;
            dm.SellerId = order.SellerId;
            dm.Status = ComplaintStatusDM.Open;
            dm.CreatedAt = DateTime.UtcNow;

            await _apiDbContext.OrderComplaint.AddAsync(dm);
            await _apiDbContext.SaveChangesAsync();

            try
            {
                if (dm.SellerId.HasValue && dm.SellerId.Value > 0)
                {
                    await _inAppNotificationProcess.NotifySeller(dm.SellerId.Value,
                        "New Complaint",
                        $"A complaint has been filed for Order #{order.OrderNumber ?? order.Id.ToString()}.",
                        "complaint", dm.Id.ToString());
                }
                await _inAppNotificationProcess.NotifyAllAdmins(
                    "New Order Complaint",
                    $"Complaint filed for Order #{order.OrderNumber ?? order.Id.ToString()}.",
                    "complaint", dm.Id.ToString());
            }
            catch { }

            return _mapper.Map<OrderComplaintSM>(dm);
        }

        #endregion

        #region User - My Complaints

        public async Task<List<OrderComplaintSM>> GetMyComplaints(long userId, int skip, int top)
        {
            var list = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            return _mapper.Map<List<OrderComplaintSM>>(list);
        }

        public async Task<IntResponseRoot> GetMyComplaintsCount(long userId)
        {
            var count = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .CountAsync(c => c.UserId == userId);
            return new IntResponseRoot(count, "Total Complaints");
        }

        #endregion

        #region Seller - Get Complaints

        public async Task<List<OrderComplaintDetailSM>> GetSellerComplaints(long sellerId, int skip, int top)
        {
            var complaints = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .Where(c => c.SellerId == sellerId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            var result = new List<OrderComplaintDetailSM>();
            foreach (var c in complaints)
            {
                result.Add(await BuildComplaintDetail(c));
            }
            return result;
        }

        public async Task<IntResponseRoot> GetSellerComplaintsCount(long sellerId)
        {
            var count = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .CountAsync(c => c.SellerId == sellerId);
            return new IntResponseRoot(count, "Total Complaints");
        }

        public async Task<OrderComplaintDetailSM> GetSellerComplaintById(long sellerId, long complaintId)
        {
            var complaint = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.SellerId == sellerId);

            if (complaint == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Complaint not found");

            return await BuildComplaintDetail(complaint);
        }

        #endregion

        #region Seller - Reply / Update Status

        public async Task<OrderComplaintSM> ReplyToComplaint(long sellerId, long complaintId, string reply, ComplaintStatusSM status)
        {
            var complaint = await _apiDbContext.OrderComplaint
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.SellerId == sellerId);

            if (complaint == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Complaint not found");

            complaint.SellerReply = reply;
            complaint.Status = (ComplaintStatusDM)status;
            complaint.RepliedAt = DateTime.UtcNow;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _apiDbContext.SaveChangesAsync();

            // Notify user about complaint reply
            try
            {
                var user = await _apiDbContext.User.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == complaint.UserId);
                if (user != null && !string.IsNullOrEmpty(user.FcmId))
                {
                    var statusText = status == ComplaintStatusSM.Resolved ? "resolved" : "updated";
                    await _notificationProcess.SendPushNotificationToUser(
                        new SendNotificationMessageSM
                        {
                            Title = $"Complaint {statusText}",
                            Message = $"Your complaint for order #{complaint.OrderId} has been {statusText}. Seller replied: \"{reply}\"",
                            AdditionalData = new Dictionary<string, string>
                            {
                                { "orderId", complaint.OrderId.ToString() },
                                { "refreshComplaints", "true" },
                                { "complaintUpdated", "true" }
                            }
                        }, user.FcmId);
                }
            }
            catch { }

            return _mapper.Map<OrderComplaintSM>(complaint);
        }

        #endregion

        #region Admin - Get All Complaints

        public async Task<List<OrderComplaintDetailSM>> GetAllComplaints(int skip, int top)
        {
            var complaints = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            var result = new List<OrderComplaintDetailSM>();
            foreach (var c in complaints)
            {
                result.Add(await BuildComplaintDetail(c));
            }
            return result;
        }

        public async Task<IntResponseRoot> GetAllComplaintsCount()
        {
            var count = await _apiDbContext.OrderComplaint.AsNoTracking().CountAsync();
            return new IntResponseRoot(count, "Total Complaints");
        }

        #endregion

        #region Private Helpers

        private async Task<OrderComplaintDetailSM> BuildComplaintDetail(OrderComplaintDM c)
        {
            var order = await _apiDbContext.Order
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == c.OrderId);

            // Customer
            var user = await _apiDbContext.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == c.UserId);

            string customerName = !string.IsNullOrWhiteSpace(user?.Name)
                ? user.Name
                : !string.IsNullOrWhiteSpace(user?.Username)
                    ? user.Username
                    : user?.Mobile;

            // Delivery address
            OrderComplaintAddressSM? addressInfo = null;
            if (order?.AddressId != null)
            {
                var addr = await _apiDbContext.UserAddress
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == order.AddressId);
                if (addr != null)
                {
                    addressInfo = new OrderComplaintAddressSM
                    {
                        Name = addr.Name,
                        Mobile = addr.Mobile,
                        Address = addr.Address,
                        Landmark = addr.Landmark,
                        Pincode = addr.Pincode,
                        City = addr.City,
                        State = addr.State
                    };
                }
            }

            // Delivery boy
            OrderComplaintDeliveryBoySM? deliveryBoyInfo = null;
            var delivery = await _apiDbContext.Deliveries
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.OrderId == c.OrderId);
            if (delivery != null)
            {
                var dBoy = await _apiDbContext.DeliveryBoy
                    .AsNoTracking()
                    .FirstOrDefaultAsync(db => db.Id == delivery.DeliveryBoyId);
                deliveryBoyInfo = new OrderComplaintDeliveryBoySM
                {
                    Id = delivery.DeliveryBoyId,
                    Name = dBoy?.Name,
                    Mobile = dBoy?.Mobile,
                    Email = dBoy?.Email,
                    DeliveryStatus = delivery.Status.ToString(),
                    AssignedAt = delivery.AssignedAt,
                    DeliveredAt = delivery.DeliveredAt
                };
            }

            // Order items
            var orderItems = await _apiDbContext.OrderItem
                .AsNoTracking()
                .Where(oi => oi.OrderId == c.OrderId)
                .ToListAsync();

            var items = new List<OrderComplaintItemSM>();
            foreach (var oi in orderItems)
            {
                var variant = await _apiDbContext.ProductVariant
                    .AsNoTracking()
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == oi.ProductVariantId);

                items.Add(new OrderComplaintItemSM
                {
                    ProductName = variant?.Product?.Name,
                    VariantName = variant?.Name,
                    Indicator = variant?.Indicator.ToString(),
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                });
            }

            return new OrderComplaintDetailSM
            {
                Id = c.Id,
                Email = c.Email,
                Message = c.Message,
                Status = ((ComplaintStatusSM)c.Status).ToString(),
                SellerReply = c.SellerReply,
                RepliedAt = c.RepliedAt,
                CreatedAt = c.CreatedAt,
                OrderId = c.OrderId,
                OrderNumber = order?.OrderNumber ?? "",
                OrderAmount = order?.Amount ?? 0,
                OrderStatus = order != null ? ((OrderStatusSM)order.OrderStatus).ToString() : "",
                PaymentStatus = order != null ? ((PaymentStatusSM)order.PaymentStatus).ToString() : "",
                PaymentMode = order != null ? ((PaymentModeSM)order.PaymentMode).ToString() : "",
                DeliveryCharge = order?.DeliveryCharge ?? 0,
                PlatformCharge = order?.PlatormCharge ?? 0,
                CutleryCharge = order?.CutlaryCharge ?? 0,
                GiftWrapCharge = order?.GiftWrapCharge ?? 0,
                LowCartFeeCharge = order?.LowCartFeeCharge ?? 0,
                TipAmount = order?.TipAmount ?? 0,
                OrderDate = order?.CreatedAt,
                UserId = c.UserId,
                CustomerName = customerName,
                CustomerMobile = user?.Mobile,
                CustomerEmail = user?.Email,
                DeliveryAddress = addressInfo,
                DeliveryBoy = deliveryBoyInfo,
                Items = items
            };
        }

        #endregion

        #region Chat Messages

        public async Task<ComplaintChatInfoSM> GetChatInfo(long complaintId, long? userId = null, long? sellerId = null)
        {
            var complaint = await _apiDbContext.OrderComplaint
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == complaintId);

            if (complaint == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            if (userId.HasValue && complaint.UserId != userId.Value)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint does not belong to you");

            if (sellerId.HasValue && complaint.SellerId != sellerId.Value)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            var messages = await _apiDbContext.ComplaintMessage
                .AsNoTracking()
                .Where(m => m.ComplaintId == complaintId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var userMsgCount = messages.Count(m => m.SenderType == "User");
            var hasImage = messages.Any(m => !string.IsNullOrEmpty(m.ImageUrl));

            // Turn-based: user can send only if last message is from Seller (or no messages yet from chat - initial complaint)
            var lastMsg = messages.LastOrDefault();
            var canUserSend = userMsgCount < MaxUserMessages
                && (complaint.Status == ComplaintStatusDM.Open || complaint.Status == ComplaintStatusDM.InProgress)
                && (lastMsg == null || lastMsg.SenderType == "Seller");

            return new ComplaintChatInfoSM
            {
                ComplaintId = complaintId,
                Status = ((ComplaintStatusSM)complaint.Status).ToString(),
                UserMessageCount = userMsgCount,
                MaxUserMessages = MaxUserMessages,
                RemainingMessages = Math.Max(0, MaxUserMessages - userMsgCount),
                CanUserSend = canUserSend,
                HasImage = hasImage,
                CanAttachImage = !hasImage,
                Messages = _mapper.Map<List<ComplaintMessageSM>>(messages)
            };
        }

        public async Task<ComplaintChatInfoSM> SendUserMessage(long userId, long complaintId, SendComplaintMessageSM sm)
        {
            var complaint = await _apiDbContext.OrderComplaint
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.UserId == userId);

            if (complaint == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            if (complaint.Status == ComplaintStatusDM.Resolved || complaint.Status == ComplaintStatusDM.Closed)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint is already resolved/closed");

            var existingMessages = await _apiDbContext.ComplaintMessage
                .AsNoTracking()
                .Where(m => m.ComplaintId == complaintId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var userMsgCount = existingMessages.Count(m => m.SenderType == "User");
            if (userMsgCount >= MaxUserMessages)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    $"You have reached the maximum of {MaxUserMessages} messages for this complaint");

            // Turn-based check: last message must be from Seller (or no messages yet)
            var lastMsg = existingMessages.LastOrDefault();
            if (lastMsg != null && lastMsg.SenderType == "User")
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Please wait for the seller to reply before sending another message");

            if (string.IsNullOrWhiteSpace(sm.Message) && string.IsNullOrWhiteSpace(sm.ImageBase64))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Please provide a message or an image");

            // Handle image (only 1 per complaint)
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(sm.ImageBase64))
            {
                var hasImage = existingMessages.Any(m => !string.IsNullOrEmpty(m.ImageUrl));
                if (hasImage)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                        "You have already attached an image to this complaint");

                imageUrl = await CompressAndUploadImage(sm.ImageBase64, sm.ImageExtension ?? "jpg");
            }

            var dm = new ComplaintMessageDM
            {
                ComplaintId = complaintId,
                SenderType = "User",
                SenderId = userId,
                Message = sm.Message,
                ImageUrl = imageUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _apiDbContext.ComplaintMessage.AddAsync(dm);

            // Update complaint status to InProgress if it was Open
            if (complaint.Status == ComplaintStatusDM.Open)
                complaint.Status = ComplaintStatusDM.InProgress;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _apiDbContext.SaveChangesAsync();

            // Notify seller
            try
            {
                if (complaint.SellerId.HasValue && complaint.SellerId.Value > 0)
                {
                    await _inAppNotificationProcess.NotifySeller(complaint.SellerId.Value,
                        "New Complaint Message",
                        $"Customer sent a new message on complaint #{complaintId}.",
                        "complaint", complaintId.ToString());
                }
            }
            catch { }

            return await GetChatInfo(complaintId, userId: userId);
        }

        public async Task<ComplaintChatInfoSM> SendSellerMessage(long sellerId, long complaintId, string message)
        {
            var complaint = await _apiDbContext.OrderComplaint
                .FirstOrDefaultAsync(c => c.Id == complaintId && c.SellerId == sellerId);

            if (complaint == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            if (string.IsNullOrWhiteSpace(message))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Message is required");

            var dm = new ComplaintMessageDM
            {
                ComplaintId = complaintId,
                SenderType = "Seller",
                SenderId = sellerId,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            await _apiDbContext.ComplaintMessage.AddAsync(dm);
            complaint.UpdatedAt = DateTime.UtcNow;
            await _apiDbContext.SaveChangesAsync();

            // Notify user via push
            try
            {
                var user = await _apiDbContext.User.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == complaint.UserId);
                if (user != null && !string.IsNullOrEmpty(user.FcmId))
                {
                    await _notificationProcess.SendPushNotificationToUser(
                        new SendNotificationMessageSM
                        {
                            Title = "Complaint Reply",
                            Message = $"Seller replied to your complaint: \"{message}\"",
                            AdditionalData = new Dictionary<string, string>
                            {
                                { "complaintId", complaintId.ToString() },
                                { "refreshComplaints", "true" }
                            }
                        }, user.FcmId);
                }
            }
            catch { }

            return await GetChatInfo(complaintId, sellerId: sellerId);
        }

        private async Task<string> CompressAndUploadImage(string base64, string extension)
        {
            // Strip data URI prefix if present
            var commaIdx = base64.IndexOf(',');
            if (commaIdx >= 0 && commaIdx < 100)
                base64 = base64.Substring(commaIdx + 1);

            // Validate it's a real image
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length > 5 * 1024 * 1024)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Image must be under 5 MB");

            var ext = (extension ?? "jpg").Trim().Replace(".", "").ToLower();
            if (ext != "jpg" && ext != "jpeg" && ext != "png" && ext != "webp")
                ext = "jpg";

            // SaveFromBase64 enforces 1MB limit and uploads to S3
            var url = await _imageProcess.SaveFromBase64(base64, ext, "content/complaints");
            if (string.IsNullOrEmpty(url))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Failed to upload image");

            return url;
        }

        #endregion Chat Messages
    }
}
