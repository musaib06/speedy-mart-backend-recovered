using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.BAL.Base.OneSignal;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.BAL.LoginUsers;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.AppUser.Login;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;
using System.Drawing;
using System.Text.Json;
using ClosedXML.Excel;

namespace Siffrum.Ecom.BAL.Product
{
    public class OrderProcess : SiffrumBalOdataBase<OrderSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly UserAddressProcess _userAddressProcess;
        private readonly NotificationProcess _notificationProcess;
        private readonly ImageProcess _imageProcess;
        private readonly StoreHoursProcess _storeHoursProcess;
        private readonly InAppNotificationProcess _inAppNotificationProcess;
        public OrderProcess(IMapper mapper, ApiDbContext apiDbContext,UserAddressProcess userAddressProcess,
            NotificationProcess notificationProcess, ImageProcess imageProcess,
            ILoginUserDetail loginUserDetail, StoreHoursProcess storeHoursProcess,
            InAppNotificationProcess inAppNotificationProcess)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _imageProcess = imageProcess;
            _userAddressProcess = userAddressProcess;
            _notificationProcess = notificationProcess;
            _storeHoursProcess = storeHoursProcess;
            _inAppNotificationProcess = inAppNotificationProcess;
        }

        #region OData
        public override async Task<IQueryable<OrderSM>> GetServiceModelEntitiesForOdata()
        {
            IQueryable<OrderDM> entitySet = _apiDbContext.Order
                .AsNoTracking();

            return await base.MapEntityAsToQuerable<OrderDM, OrderSM>(_mapper, entitySet);
        }
        #endregion

        #region USER - CREATE ORDER       
            

        public async Task<OrderSM> CreateOrderAsync(
    OrderSM orderSM,
    List<OrderItemSM> itemSMs)
        {
            if (itemSMs == null || !itemSMs.Any())
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order must contain at least one item");
            if (!orderSM.SellerId.HasValue || orderSM.SellerId.Value <= 0)
            {
                var assignedSellerId = await _apiDbContext.User
                    .AsNoTracking()
                    .Where(u => u.Id == orderSM.UserId)
                    .Select(u => u.AssignedSellerId)
                    .FirstOrDefaultAsync();

                if (assignedSellerId.HasValue && assignedSellerId.Value > 0)
                    orderSM.SellerId = assignedSellerId.Value;
            }

            var defaultAddress = await _userAddressProcess.GetDefaultAddress(orderSM.UserId);
            if (defaultAddress == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"User with Id: {orderSM.UserId} has no default address"
                    , "Please set a default address before placing an order");
            }

            // 🔹 Store hours validation
            if (orderSM.SellerId.HasValue && orderSM.SellerId.Value > 0)
            {
                var availability = await _storeHoursProcess.CheckStoreAvailability(orderSM.SellerId.Value);
                if (!availability.IsOpen)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, availability.Message);
            }

            await using var transaction = await _apiDbContext.Database.BeginTransactionAsync();

            OrderDM orderDM;

            try
            {

                orderDM = _mapper.Map<OrderDM>(orderSM);

                orderDM.OrderNumber = await GenerateUniqueOrderNumber();
                orderDM.TransactionId = await GenerateUniqueTransactionId();
                orderDM.Receipt = $"Receipt#{Guid.NewGuid():N}";
                orderDM.PaymentStatus = PaymentStatusDM.Pending;
                orderDM.OrderStatus = orderDM.PaymentMode == PaymentModeDM.Online
                    ? OrderStatusDM.AwaitingPayment
                    : OrderStatusDM.Created;
                orderDM.PaidAmount = 0;
                orderDM.RazorpayOrderId = null;
                orderDM.RazorpayPaymentId = null;
                orderDM.AddressId = defaultAddress.Id;
                orderDM.SellerId = orderSM.SellerId;
                // ✅ Use frontend amount directly
                orderDM.DueAmount = orderDM.Amount;

                var orderItemDMs = _mapper.Map<List<OrderItemDM>>(itemSMs);

                // Serialize toppings & addons from the request lists into JSON strings for DB
                for (int i = 0; i < orderItemDMs.Count; i++)
                {
                    var src = itemSMs[i];
                    if (src.SelectedToppings != null && src.SelectedToppings.Any())
                        orderItemDMs[i].SelectedToppings = JsonSerializer.Serialize(src.SelectedToppings, _jsonOptions);
                    if (src.SelectedAddons != null && src.SelectedAddons.Any())
                        orderItemDMs[i].SelectedAddons = JsonSerializer.Serialize(src.SelectedAddons, _jsonOptions);
                }

                // 🔹 Load variants with product for tax + stock
                var variantIds = orderItemDMs.Select(x => x.ProductVariantId).Distinct().ToList();
                var variants = await _apiDbContext.ProductVariant
                    .Include(v => v.Product)
                    .Where(v => variantIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id);

                // ✅ Calculate tax server-side from product TaxPercentage (same as cart)
                decimal totalTax = 0;
                foreach (var item in orderItemDMs)
                {
                    item.TotalPrice = item.UnitPrice * item.Quantity;
                    if (variants.TryGetValue(item.ProductVariantId, out var v))
                    {
                        var taxPct = v.Product?.TaxPercentage ?? 0;
                        totalTax += Math.Round(item.TotalPrice * taxPct / 100m, 2);
                    }
                }
                orderDM.TaxAmount = totalTax;

                await _apiDbContext.Order.AddAsync(orderDM);
                await _apiDbContext.SaveChangesAsync();

                // 🔹 Stock validation & deduction
                var isOnlinePayment = orderDM.PaymentMode == PaymentModeDM.Online;

                foreach (var item in orderItemDMs)
                {
                    item.OrderId = orderDM.Id;

                    if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                        throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                            "Product not found");

                    if (variant.Stock.HasValue && variant.Stock.Value < item.Quantity)
                        throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                            $"Only {(int)variant.Stock.Value} in stock for this product");

                    // Only deduct stock for COD; online orders deduct after payment confirmed
                    if (!isOnlinePayment && variant.Stock.HasValue)
                    {
                        variant.Stock -= item.Quantity;
                        variant.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _apiDbContext.OrderItem.AddRangeAsync(orderItemDMs);

                var invoice = new InvoiceDM
                {
                    TransactionId = orderDM.TransactionId,
                    InvoiceDate = DateTime.UtcNow,
                    OrderId = orderDM.Id,
                    Currency = orderDM.Currency,
                    Amount = orderDM.Amount, // ✅ frontend amount
                    PaymentStatus = PaymentStatusDM.Pending,
                    OrderStatus = OrderStatusDM.Created
                };

                await _apiDbContext.Invoice.AddAsync(invoice);

                await _apiDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (SiffrumException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log,
                    $"Message: {ex.Message}, InnerException: {ex.InnerException?.Message}",
                    "Failed to create order. Please try again later.");
            }

            // ✅ Notifications — skip for online payment (will fire after payment confirmed via webhook)
            if (orderDM.PaymentMode != PaymentModeDM.Online)
            {
                try
                {
                    if (orderDM.SellerId.HasValue && orderDM.SellerId.Value > 0)
                    {
                        var sellerNotification = new SendNotificationMessageSM()
                        {
                            UserIds = new List<long> { orderDM.SellerId.Value },
                            Title = "New Order Received",
                            Message = $"You have received a new order ({orderDM.OrderNumber}). Please accept or reject it.",
                            AdditionalData = new Dictionary<string, string>
                            {
                                { "orderId", orderDM.Id.ToString() },
                                { "refreshOrders", "true" }
                            }
                        };
                        await _notificationProcess.SendBulkPushNotificationToSellerForOrder(sellerNotification);
                        await _inAppNotificationProcess.NotifySeller(orderDM.SellerId.Value,
                            "New Order Received",
                            $"Order #{orderDM.OrderNumber} — ₹{orderDM.Amount}. Please accept or reject.",
                            "new_order", orderDM.Id.ToString());
                    }
                }
                catch
                {
                    // Don't fail order if notification fails
                }

                // Low stock alerts for seller
                try
                {
                    if (orderDM.SellerId.HasValue && orderDM.SellerId.Value > 0)
                    {
                        var lowStockVariants = await _apiDbContext.ProductVariant
                            .Include(v => v.Product)
                            .Where(v => v.Product.SellerId == orderDM.SellerId.Value &&
                                        v.Stock.HasValue && v.Stock <= 10 && v.Stock > 0)
                            .ToListAsync();

                        foreach (var v in lowStockVariants)
                        {
                            await _inAppNotificationProcess.NotifySeller(orderDM.SellerId.Value,
                                "Low Stock Alert",
                                $"{v.Product?.Name ?? "Product"} ({v.Name}) has only {(int)v.Stock} left in stock.",
                                "low_stock", v.Id.ToString());
                        }
                    }
                }
                catch { }
            }

            return await GetOrderSM(orderDM);
        }

        #endregion

        #region ORDER MANAGEMENT

        private static readonly Dictionary<string, OrderStatusDM[]> _statusMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pending"] = new[] { OrderStatusDM.Created, OrderStatusDM.Processing },
            ["accepted"] = new[] { OrderStatusDM.SellerAccepted },
            ["completed"] = new[] { OrderStatusDM.Delivered },
            ["cancelled"] = new[] { OrderStatusDM.Cancelled, OrderStatusDM.CancelledBySeller },
        };

        private IQueryable<OrderDM> ApplySellerOrderFilters(
            IQueryable<OrderDM> query, string? status, DateTime? dateFrom, DateTime? dateTo, PaymentModeSM? paymentMode = null)
        {
            if (!string.IsNullOrEmpty(status) && _statusMap.TryGetValue(status, out var statuses))
                query = query.Where(x => statuses.Contains(x.OrderStatus));

            if (paymentMode.HasValue)
                query = query.Where(x => x.PaymentMode == (PaymentModeDM)(int)paymentMode.Value);

            if (dateFrom.HasValue)
            {
                // The input is IST datetime from frontend
                // Data in DB is stored as UTC but with IST hour values (e.g., 03:03 UTC = 03:03 AM display)
                // So we treat the IST input as UTC for comparison
                var utcFrom = dateFrom.Value;
                Console.WriteLine($"[DEBUG] dateFrom input: {dateFrom.Value:yyyy-MM-dd HH:mm:ss.fff} (Kind: {dateFrom.Value.Kind}), using as UTC: {utcFrom:yyyy-MM-dd HH:mm:ss.fff}");
                query = query.Where(x => x.CreatedAt >= utcFrom);
            }
            if (dateTo.HasValue)
            {
                var utcTo = dateTo.Value;
                Console.WriteLine($"[DEBUG] dateTo input: {dateTo.Value:yyyy-MM-dd HH:mm:ss.fff} (Kind: {dateTo.Value.Kind}), using as UTC: {utcTo:yyyy-MM-dd HH:mm:ss.fff}");
                query = query.Where(x => x.CreatedAt < utcTo);
            }
            return query;
        }

        public async Task<List<OrderSM>> GetSellerOrders(
            long sellerId, int skip, int top,
            string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null, PaymentModeSM? paymentMode = null)
        {
            var query = _apiDbContext.Order
                .AsNoTracking()
                .Where(x => x.SellerId == sellerId && x.OrderStatus != OrderStatusDM.AwaitingPayment);

            query = ApplySellerOrderFilters(query, status, dateFrom, dateTo, paymentMode);

            // Debug: Log the SQL that will be executed
            var sql = query.ToQueryString();
            Console.WriteLine($"[DEBUG SQL] {sql.Substring(0, Math.Min(500, sql.Length))}...");

            var orders = await query
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync();
            
            Console.WriteLine($"[DEBUG] Found {orders.Count} orders for seller {sellerId}");
            foreach (var o in orders)
            {
                Console.WriteLine($"[DEBUG ORDER] ID:{o.Id}, CreatedAt:{o.CreatedAt:yyyy-MM-dd HH:mm:ss.fff}, Status:{o.OrderStatus}");
            }

            var orderList = new List<OrderSM>();
            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }

        public async Task<IntResponseRoot> GetSellerOrdersCount(
            long sellerId,
            string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null, PaymentModeSM? paymentMode = null)
        {
            var query = _apiDbContext.Order
                .AsNoTracking()
                .Where(x => x.SellerId == sellerId && x.OrderStatus != OrderStatusDM.AwaitingPayment);

            query = ApplySellerOrderFilters(query, status, dateFrom, dateTo, paymentMode);

            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total seller orders");
        }

        public async Task<OrderSM> SellerAcceptOrder(long orderId, long sellerId, int preparationTimeInMinutes = 0)
        {
            var order = await _apiDbContext.Order
                .FirstOrDefaultAsync(x => x.Id == orderId && x.SellerId == sellerId);

            if (order == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found or does not belong to this seller");

            if (order.OrderStatus != OrderStatusDM.Created && order.OrderStatus != OrderStatusDM.Processing)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    $"Order cannot be accepted. Current status: {order.OrderStatus}");

            order.OrderStatus = OrderStatusDM.SellerAccepted;
            order.PreparationTimeInMinutes = preparationTimeInMinutes > 0 ? preparationTimeInMinutes : 0;
            order.SellerAcceptedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            await _apiDbContext.SaveChangesAsync();

            // Notify user
            try
            {
                var user = await _apiDbContext.User.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == order.UserId);
                Console.WriteLine($"[NOTIFY-USER] OrderAccept userId={order.UserId}, userFound={user != null}, fcmId={user?.FcmId?.Length ?? 0} chars");
                if (user != null && !string.IsNullOrEmpty(user.FcmId))
                {
                    var prepMsg = preparationTimeInMinutes > 0
                        ? $"Estimated preparation time: {preparationTimeInMinutes} minutes."
                        : "";
                    var userNotification = new SendNotificationMessageSM
                    {
                        Title = "Order Accepted ✅",
                        Message = $"Your order #{order.OrderNumber ?? order.Id.ToString()} has been accepted and is being prepared! {prepMsg}",
                        AdditionalData = new Dictionary<string, string>
                        {
                            { "orderId", order.Id.ToString() },
                            { "orderAccept", "true" }
                        }
                    };
                    var result = await _notificationProcess.SendPushNotificationToUser(userNotification, user.FcmId);
                    Console.WriteLine($"[NOTIFY-USER] OrderAccept result: {result?.BoolResponse} - {result?.ResponseMessage}");
                }
            }
            catch (Exception ex) { Console.WriteLine($"[NOTIFY-USER] OrderAccept EXCEPTION: {ex.Message}"); }

            // Notify seller's own delivery boys
            try
            {
                if (order.SellerId.HasValue && order.SellerId.Value > 0)
                {
                    var orderLabel = order.OrderNumber ?? order.Id.ToString();

                    // Push notification
                    var dboyNotification = new SendNotificationMessageSM
                    {
                        Title = "New Order Available",
                        Message = $"Order ({orderLabel}) is ready for pickup. Accept now!",
                        AdditionalData = new Dictionary<string, string>
                        {
                            { "orderId", order.Id.ToString() },
                            { "orderNumber", order.OrderNumber ?? "" },
                            { "type", "new_order" }
                        }
                    };
                    var pushResult = await _notificationProcess.SendPushNotificationToSellerDeliveryBoys(
                        dboyNotification, order.SellerId.Value);
                    Console.WriteLine($"[ORDER] Push to delivery boys for order #{orderLabel}: {pushResult?.ResponseMessage}");

                    // In-app notification (works even without device token)
                    await _inAppNotificationProcess.NotifySellerDeliveryBoys(
                        order.SellerId.Value,
                        "New Order Available 🛵",
                        $"Order #{orderLabel} is ready for pickup. Accept now!",
                        "new_order", order.Id.ToString());
                }
            }
            catch (Exception ex) { Console.WriteLine($"[ORDER] Error notifying delivery boys: {ex.Message}"); }

            return await GetOrderSM(order);
        }

        public async Task<OrderSM> SellerCancelOrder(long orderId, long sellerId)
        {
            var order = await _apiDbContext.Order
                .FirstOrDefaultAsync(x => x.Id == orderId && x.SellerId == sellerId);

            if (order == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found or does not belong to this seller");

            if (order.OrderStatus == OrderStatusDM.Delivered
                || order.OrderStatus == OrderStatusDM.Cancelled
                || order.OrderStatus == OrderStatusDM.CancelledBySeller)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,
                    $"Order cannot be cancelled. Current status: {order.OrderStatus}");

            order.OrderStatus = OrderStatusDM.CancelledBySeller;
            order.UpdatedAt = DateTime.UtcNow;

            // 🔹 Restore stock
            await RestoreStockForOrder(order.Id);

            if (order.PaymentMode == PaymentModeDM.Online && order.PaymentStatus == PaymentStatusDM.Paid)
            {
                order.PaymentStatus = PaymentStatusDM.RefundInitiated;
            }
            else
            {
                order.PaymentStatus = PaymentStatusDM.Cancelled;
            }

            await _apiDbContext.SaveChangesAsync();

            // Notify user about cancellation
            try
            {
                var user = await _apiDbContext.User.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (user != null && !string.IsNullOrEmpty(user.FcmId))
                {
                    var cancelMsg = order.PaymentMode == PaymentModeDM.Online && order.PaymentStatus == PaymentStatusDM.RefundInitiated
                        ? $"Your order ({order.OrderNumber ?? order.Id.ToString()}) has been cancelled by the seller. Your refund will be initiated shortly."
                        : $"Your order ({order.OrderNumber ?? order.Id.ToString()}) has been cancelled by the seller. No payment is required.";

                    var userNotification = new SendNotificationMessageSM
                    {
                        Title = "Order Cancelled by Seller",
                        Message = cancelMsg,
                        AdditionalData = new Dictionary<string, string>
                        {
                            { "orderId", order.Id.ToString() },
                            { "refreshOrders", "true" }
                        }
                    };
                    await _notificationProcess.SendPushNotificationToUser(userNotification, user.FcmId);
                }
            }
            catch { }

            return await GetOrderSM(order);
        }

        #endregion

        #region USER - GET MY ORDERS

        public async Task<List<OrderSM>> GetMyOrdersAsync(
            long userId,
            int skip,
            int top)
        {
            var query = _apiDbContext.Order.AsNoTracking().Where(x => x.UserId == userId);
            var orders = await query
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync();
            var orderList = new List<OrderSM>();
            foreach(var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }       

        public async Task<IntResponseRoot> GetMyOrdersCountAsync(
           long userId)
        {
            var count = await _apiDbContext.Order.AsNoTracking()
                .Where(x => x.UserId == userId)
                .CountAsync();

            return new IntResponseRoot(count, "Total Orders");
        }

        public async Task<BoolResponseRoot> IsMyFirstOrderApplicable(
          long userId)
        {
            var user = await _apiDbContext.User.FindAsync(userId);
            if(user == null || user?.Status == StatusDM.Inactive)
            {
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog,$"User with Id: {userId} not found or checking delivery applicability", "User not found");
            }
            if (string.IsNullOrEmpty(user.FriendsCode))
            {
                return new BoolResponseRoot(false, "User friends referral code not found");
            }
            var count = await _apiDbContext.Order
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.PaymentStatus == PaymentStatusDM.Paid)
                .CountAsync();
            if(count > 0)
            {
                return new BoolResponseRoot(false, $"Users has already {count} orders");

            }

            return new BoolResponseRoot(true, "Users First Order");
        }

        public async Task<List<OrderSM>> GetUserOrdersByOrderTye(PaymentStatusSM paymentStatus,
            long userId,
            int skip,
            int top)
        {
            var orders = await _apiDbContext.Order.AsNoTracking()
                .Where(x => x.UserId == userId && x.OrderStatus == (OrderStatusDM)paymentStatus)
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            var orderList = new List<OrderSM>();
            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }
        public async Task<IntResponseRoot> GetUserOrdersByOrderTyeCount(PaymentStatusSM paymentStatus,
            long userId)
        {
            var count = await _apiDbContext.Order.AsNoTracking()
                .Where(x => x.UserId == userId && x.OrderStatus == (OrderStatusDM)paymentStatus)
                .CountAsync();

            return new IntResponseRoot(count, "Total User Orders");
        }

        public async Task<List<OrderSM>> SearchOrder(
            long? id,
            PaymentStatusSM? paymentStatus,
            OrderStatusSM? orderStatus,
            int skip,int top)
        {
            IQueryable<OrderDM> query = _apiDbContext.Order
                .AsNoTracking();

            // Filter by Id
            if (id.HasValue && id.Value > 0)
            {
                query = query.Where(x => x.Id == id.Value);
            }

            // Filter by PaymentStatus
            if (paymentStatus.HasValue)
            {
                query = query.Where(x => x.PaymentStatus == (PaymentStatusDM)paymentStatus.Value);
            }

            // Filter by OrderStatus
            if (orderStatus.HasValue)
            {
                query = query.Where(x => x.OrderStatus == (OrderStatusDM)orderStatus.Value);
            }

            var orders = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            var orderList = new List<OrderSM>();

            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }

            return orderList;
        }

        public async Task<IntResponseRoot> SearchOrderCount(
           long? id,
           PaymentStatusSM? paymentStatus,
           OrderStatusSM? orderStatus)
        {
            IQueryable<OrderDM> query = _apiDbContext.Order
                .AsNoTracking();

            // Filter by Id
            if (id.HasValue && id.Value > 0)
            {
                query = query.Where(x => x.Id == id.Value);
            }

            // Filter by PaymentStatus
            if (paymentStatus.HasValue)
            {
                query = query.Where(x => x.PaymentStatus == (PaymentStatusDM)paymentStatus.Value);
            }

            // Filter by OrderStatus
            if (orderStatus.HasValue)
            {
                query = query.Where(x => x.OrderStatus == (OrderStatusDM)orderStatus.Value);
            }

            var count = await query
                .CountAsync();
            return new IntResponseRoot(count, "Total Count");
        }

        #region Advanced Search (Admin)

        private IQueryable<OrderDM> BuildAdvancedQuery(
            long? id,
            OrderStatusSM? orderStatus,
            PaymentStatusSM? paymentStatus,
            PaymentModeSM? paymentMode,
            long? sellerId,
            string? search,
            DateTime? dateFrom,
            DateTime? dateTo,
            decimal? minAmount,
            decimal? maxAmount)
        {
            IQueryable<OrderDM> query = _apiDbContext.Order.AsNoTracking();

            if (id.HasValue && id.Value > 0)
                query = query.Where(x => x.Id == id.Value);

            if (orderStatus.HasValue)
                query = query.Where(x => x.OrderStatus == (OrderStatusDM)(int)orderStatus.Value);

            if (paymentStatus.HasValue)
                query = query.Where(x => x.PaymentStatus == (PaymentStatusDM)(int)paymentStatus.Value);

            if (paymentMode.HasValue)
                query = query.Where(x => x.PaymentMode == (PaymentModeDM)(int)paymentMode.Value);

            if (sellerId.HasValue && sellerId.Value > 0)
                query = query.Where(x => x.SellerId == sellerId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                var userIds = _apiDbContext.User
                    .AsNoTracking()
                    .Where(u => u.Name.ToLower().Contains(term)
                             || u.Username.ToLower().Contains(term)
                             || u.Mobile.Contains(term))
                    .Select(u => u.Id);

                query = query.Where(x =>
                    x.OrderNumber.ToLower().Contains(term) ||
                    (x.Receipt != null && x.Receipt.ToLower().Contains(term)) ||
                    userIds.Contains(x.UserId));
            }

            if (dateFrom.HasValue)
            {
                var dayStart = IstDateHelper.IstDayStartUtc(dateFrom.Value.Date);
                query = query.Where(x => x.CreatedAt >= dayStart);
            }

            if (dateTo.HasValue)
            {
                var dayEnd = IstDateHelper.IstDayEndUtc(dateTo.Value.Date);
                query = query.Where(x => x.CreatedAt < dayEnd);
            }

            if (minAmount.HasValue)
                query = query.Where(x => x.Amount >= minAmount.Value);

            if (maxAmount.HasValue)
                query = query.Where(x => x.Amount <= maxAmount.Value);

            return query;
        }

        public async Task<List<OrderSM>> AdvancedSearchOrders(
            long? id,
            OrderStatusSM? orderStatus,
            PaymentStatusSM? paymentStatus,
            PaymentModeSM? paymentMode,
            long? sellerId,
            string? search,
            DateTime? dateFrom,
            DateTime? dateTo,
            decimal? minAmount,
            decimal? maxAmount,
            int skip, int top)
        {
            var query = BuildAdvancedQuery(id, orderStatus, paymentStatus, paymentMode,
                sellerId, search, dateFrom, dateTo, minAmount, maxAmount);

            var orders = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            var orderList = new List<OrderSM>();
            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }

        public async Task<IntResponseRoot> AdvancedSearchOrdersCount(
            long? id,
            OrderStatusSM? orderStatus,
            PaymentStatusSM? paymentStatus,
            PaymentModeSM? paymentMode,
            long? sellerId,
            string? search,
            DateTime? dateFrom,
            DateTime? dateTo,
            decimal? minAmount,
            decimal? maxAmount)
        {
            var query = BuildAdvancedQuery(id, orderStatus, paymentStatus, paymentMode,
                sellerId, search, dateFrom, dateTo, minAmount, maxAmount);

            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total Count");
        }

        #endregion Advanced Search

        public async Task<List<OrderItemSM>> GetOrdersItemsAsync(
            long userId,
            long orderId,
            bool isSuperAdmin,
            int skip,
            int top)
        {
            var query = _apiDbContext.Order
                .AsNoTracking().AsQueryable();
            if (!isSuperAdmin)
            {
                query = query.Where(query => query.UserId == userId);
            }
            var order = await query.FirstOrDefaultAsync(x => x.Id == orderId);
            if(order == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log,$"User tried to access order with different user or order not found for Id: {orderId}",
                    "Order not found");
            }
            var orderItems = await _apiDbContext.OrderItem.Where(x => x.OrderId == orderId)
                .Skip(skip).Take(top)
                .ToListAsync();
            var orderItemList = new List<OrderItemSM>();
            foreach (var item in orderItems)
            {
                var orderItem = await GetOrderItemSM(item);
                orderItemList.Add(orderItem);
            }
            return orderItemList;

        }

        public async Task<IntResponseRoot> GetOrdersItemsCountAsync(
           long userId,
            long orderId,
            bool isSuperAdmin)
        {
            var query = _apiDbContext.Order
                .AsNoTracking().AsQueryable();
            if (!isSuperAdmin)
            {
                query = query.Where(query => query.UserId == userId);
            }
            var order = await query.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log, $"User tried to access order with different user or order not found for Id: {orderId}",
                    "Order not found");
            }
            var count = await _apiDbContext.OrderItem.Where(x => x.OrderId == orderId)
                .CountAsync();
            return new IntResponseRoot(count, "Total Orders");
        }
        public async Task<OrderSM> GetOrderByOrderId(
            long orderId)
        {
            var order = await _apiDbContext.Order.FindAsync(orderId);
            if(order == null)
            {
                return null;
            }

            return await GetOrderSM(order);
        }
        public async Task<List<OrderItemSM>> GetOrdersItemByOrderId(
            long orderId)
        {            
            var orderItems = await _apiDbContext.OrderItem.Where(x => x.OrderId == orderId)
                .ToListAsync();

            var orderItemList = new List<OrderItemSM>();
            foreach (var item in orderItems)
            {
                var orderItem = await GetOrderItemSM(item);
                orderItemList.Add(orderItem);
            }
            return orderItemList;
        }

        public async Task<OrderItemSM> GetOrdersItemByOrderItemId(
            long orderItemId)
        {
            var orderItem = await _apiDbContext.OrderItem.FindAsync(orderItemId);

            return await GetOrderItemSM(orderItem);
        }

        public async Task<OrderItemExtendedDetailsSM> GetOrdersItemByOrderItemIdWithStatusDetails(
    long orderItemId)
        {
            var orderItem = await _apiDbContext.OrderItem
                .AsNoTracking()
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == orderItemId);

            if (orderItem == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    $"OrderItem with Id {orderItemId} not found",
                    "Order item not found");
            }
            var response = new OrderItemExtendedDetailsSM();

            var sm = await GetOrderItemSM(orderItem);

            response.OrderItem = sm;

            // ✅ Get statuses from Order table
            response.OrderStatus = (OrderStatusSM)orderItem.Order?.OrderStatus;
            response.PaymentStatus = (PaymentStatusSM)orderItem.Order?.PaymentStatus;

            return response;
        }



        public async Task<List<OrderItemSM>> GetOrdersItemsByOrderId(
            long orderId)
        {
            var order = await _apiDbContext.Order.FindAsync(orderId);
            
            if (order == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log, $"Order not found for Id: {orderId}",
                    "Order not found");
            }
            var orderItems = await _apiDbContext.OrderItem.Where(x => x.OrderId == orderId)
                .ToListAsync();

            var orderItemList = new List<OrderItemSM>();
            foreach (var item in orderItems)
            {
                var orderItem = await GetOrderItemSM(item);
                orderItemList.Add(orderItem);
            }
            return orderItemList;
        }

        public async Task<List<OrderItemDetailSM>> GetOrderItemsDetailByOrderId(long orderId)
        {
            var order = await _apiDbContext.Order.FindAsync(orderId);
            if (order == null)
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"Order not found for Id: {orderId}", "Order not found");

            var orderItems = await _apiDbContext.OrderItem.Where(x => x.OrderId == orderId).ToListAsync();
            var result = new List<OrderItemDetailSM>();
            foreach (var item in orderItems)
            {
                result.Add(await GetOrderItemDetailSM(item));
            }
            return result;
        }

        public async Task<List<OrderItemSM>> GetSellerOrdersItemsAsync(
            long sellerId,
            int skip,
            int top)
        {
            
            var orderItems = await _apiDbContext.OrderItem.Where(x => x.ProductVariant.Product.SellerId == sellerId)
                .OrderBy(x => x.OrderId)
                .Skip(skip).Take(top)
                .ToListAsync();

            var orderItemList = new List<OrderItemSM>();
            foreach (var item in orderItems)
            {
                var orderItem = await GetOrderItemSM(item);
                orderItemList.Add(orderItem);
            }
            return orderItemList;
        }

        public async Task<IntResponseRoot> GetSellerOrdersItemsCountAsync(
           long sellerId)
        {

            var count = await _apiDbContext.OrderItem
                .Where(x => x.ProductVariant.Product.SellerId == sellerId)
                .CountAsync();
            return new IntResponseRoot(count, "Total Orders");
        }


        public async Task<OrderSM> GetMyOrderByIdAsync(long id, long userId)
        {
            var order = await _apiDbContext.Order
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (order == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found");

            return await GetOrderSM(order);
        }

        #endregion

        #region ADMIN - GET ALL

        public async Task<List<OrderSM>> GetAllAsync(int skip, int top)
        {
            var orders = await _apiDbContext.Order
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            var orderList = new List<OrderSM>();
            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }

        public async Task<IntResponseRoot> GetCountAsync()
        {
            var count = await _apiDbContext.Order.CountAsync();
            return new IntResponseRoot(count, "Total Orders");
        }

        public async Task<List<OrderSM>> GetAllByOrderStatusAsync(OrderStatusSM status, int skip, int top)
        {
            var orders = await _apiDbContext.Order
                .AsNoTracking()
                .Where(x=>x.OrderStatus == (OrderStatusDM)status)
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            var orderList = new List<OrderSM>();
            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }

        public async Task<IntResponseRoot> GetByOrderStatusCountAsync(OrderStatusSM status)
        {
            var count = await _apiDbContext.Order
                .Where(x => x.OrderStatus == (OrderStatusDM)status)
                .CountAsync();
            return new IntResponseRoot(count, "Total Orders");
        }

        public async Task<List<OrderSM>> GetAllByPaymentStatusAsync(PaymentStatusSM status, int skip, int top)
        {
            var orders = await _apiDbContext.Order
                .AsNoTracking()
                .Where(x => x.PaymentStatus == (PaymentStatusDM)status)
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(top)
                .ToListAsync();

            var orderList = new List<OrderSM>();
            foreach (var order in orders)
            {
                var orderSM = await GetOrderSM(order);
                orderList.Add(orderSM);
            }
            return orderList;
        }

        public async Task<IntResponseRoot> GetByPaymentStatusCountAsync(PaymentStatusSM status)
        {
            var count = await _apiDbContext.Order
                .Where(x => x.PaymentStatus == (PaymentStatusDM)status)
                .CountAsync();
            return new IntResponseRoot(count, "Total Orders");
        }

        public async Task<OrderSM> GetByIdAsync(long id)
        {
            var order = await _apiDbContext.Order
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
            {
                return null;
            }

            return await GetOrderSM(order);
        }

        #endregion

        #region UPDATE STATUS (ADMIN)

        public async Task<BoolResponseRoot> UpdateOrderStatusAsync(
            long id,
            OrderStatusSM status)
        {
            var order = await _apiDbContext.Order
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found");

            order.OrderStatus = (OrderStatusDM)status;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = _loginUserDetail.LoginId;
            await _apiDbContext.SaveChangesAsync();

            // Notify user about order status change
            try
            {
                var user = await _apiDbContext.User.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == order.UserId);
                if (user != null && !string.IsNullOrEmpty(user.FcmId))
                {
                    var statusText = status switch
                    {
                        OrderStatusSM.Processing => "is being processed",
                        OrderStatusSM.SellerAccepted => "has been accepted by the seller",
                        OrderStatusSM.Assigned => "has been assigned for delivery",
                        OrderStatusSM.PickedUp => "has been picked up",
                        OrderStatusSM.OutForDelivery => "is out for delivery",
                        OrderStatusSM.Delivered => "has been delivered",
                        OrderStatusSM.Cancelled => "has been cancelled",
                        OrderStatusSM.CancelledBySeller => "has been cancelled by the seller",
                        OrderStatusSM.Failed => "delivery has failed",
                        _ => $"status updated to {status}"
                    };
                    await _notificationProcess.SendPushNotificationToUser(
                        new SendNotificationMessageSM
                        {
                            Title = "Order Update",
                            Message = $"Your order #{order.OrderNumber ?? order.Id.ToString()} {statusText}.",
                            AdditionalData = new Dictionary<string, string>
                            {
                                { "orderId", order.Id.ToString() },
                                { "refreshOrders", "true" }
                            }
                        }, user.FcmId);
                }
            }
            catch { }

            return new BoolResponseRoot(true, "Order status updated");
        }

        public async Task<BoolResponseRoot> UpdatePaymentStatusAsync(
            long id,
            PaymentStatusSM status)
        {
            var order = await _apiDbContext.Order
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found");

            order.PaymentStatus = (PaymentStatusDM)status;

            if (status == PaymentStatusSM.Paid)
            {
                order.PaidAmount = order.Amount;
                order.DueAmount = 0;
            }
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = _loginUserDetail.LoginId;
            await _apiDbContext.SaveChangesAsync();

            return new BoolResponseRoot(true, "Payment status updated");
        }

        #endregion

        #region USER - CANCEL

        public async Task<BoolResponseRoot> CancelOrderAsync(
            long id,
            long userId)
        {
            var order = await _apiDbContext.Order
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (order == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found");

            if (order.OrderStatus != OrderStatusDM.Created && order.OrderStatus != OrderStatusDM.Processing)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order can only be cancelled before seller accepts");

            order.OrderStatus = OrderStatusDM.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = _loginUserDetail.LoginId;

            // 🔹 Restore stock
            await RestoreStockForOrder(order.Id);

            await _apiDbContext.SaveChangesAsync();

            // Notify seller about user cancellation
            try
            {
                if (order.SellerId.HasValue)
                {
                    var seller = await _apiDbContext.Seller.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == order.SellerId.Value);
                    if (seller != null && !string.IsNullOrEmpty(seller.FcmId))
                    {
                        await _notificationProcess.SendPushNotificationByPlayerId(seller.FcmId,
                            new SendNotificationMessageSM
                            {
                                Title = "Order Cancelled by Customer",
                                Message = $"Order #{order.OrderNumber ?? order.Id.ToString()} has been cancelled by the customer.",
                                AdditionalData = new Dictionary<string, string>
                                {
                                    { "orderId", order.Id.ToString() },
                                    { "refreshOrders", "true" }
                                }
                            });
                    }
                }
            }
            catch { }

            return new BoolResponseRoot(true, "Order cancelled successfully");
        }

        #endregion

        #region DELETE (ADMIN)

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var order = await _apiDbContext.Order
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (order == null)
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Order not found");

            _apiDbContext.OrderItem.RemoveRange(order.OrderItems);
            _apiDbContext.Order.Remove(order);

            await _apiDbContext.SaveChangesAsync();

            return new DeleteResponseRoot(true, "Order deleted successfully");
        }

        #endregion

        #region Product Availability

        public async Task<BoolResponseRoot> IsProductOrderPossible(long userId, long productVariantId)
        {
            var userDefaultAddress = await _userAddressProcess.GetDefaultAddress(userId);

            if (userDefaultAddress == null )
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log,
                    $"User with Id: {userId} has no default address",
                    "Please update the default address. Address is required for order");
            }
            var sellerId = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .Where(x => x.Id == productVariantId && x.Status == ProductStatusDM.Active)
                .Select(x => x.Product.SellerId)
                .FirstOrDefaultAsync();

            if (sellerId == 0)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    $"User with Id: {userId} tried to acces product with Id: {productVariantId} which is not found",
                    $"ProductVariant not found");
            }

            return new BoolResponseRoot(true, "Delivery available at your location");
        }

        #endregion Product Availability

        #region Order Address

        public async Task<UserAddressSM> GetOrderAddress(long orderId)
        {
            var order = await _apiDbContext.Order.FindAsync(orderId);
            if (order == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"Order with Id: {orderId} not found or getting address", "Order not found");
            }
            var address = await _userAddressProcess.GetById((long)order.AddressId);
            return address;
        }

        #endregion Order Address

        #region Order Delivery Info

        public async Task<OrderDeliveryInfoSM> GetOrderDeliveryInfo(long orderId)
        {
            var delivery = await _apiDbContext.Deliveries
                .Include(d => d.DeliveryBoy)
                .Where(d => d.OrderId == orderId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();

            if (delivery == null)
                return null;

            return new OrderDeliveryInfoSM
            {
                DeliveryBoyId = delivery.DeliveryBoyId,
                DeliveryBoyName = delivery.DeliveryBoy?.Name,
                DeliveryBoyMobile = delivery.DeliveryBoy?.Mobile,
                DeliveryStatus = delivery.Status.ToString(),
                AssignedAt = delivery.AssignedAt,
                DeliveredAt = delivery.DeliveredAt
            };
        }

        #endregion Order Delivery Info

        #region Get OrderSM

        public async Task<OrderSM> GetOrderSM(OrderDM dm)
        {
            var user = _apiDbContext.User.Where(x => x.Id == dm.UserId).FirstOrDefault();

            string displayName = !string.IsNullOrWhiteSpace(user?.Name)
                ? user.Name
                : !string.IsNullOrWhiteSpace(user?.Username)
                    ? user.Username
                    : user?.Mobile;

            var sm = _mapper.Map<OrderSM>(dm);
            sm.CustomerName = displayName;

            // Fetch delivery record for this order (used for both prep status and delivery boy info)
            var delivery = await _apiDbContext.Deliveries
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.OrderId == dm.Id);

            if (delivery != null)
            {
                sm.DeliveryBoyId = delivery.DeliveryBoyId;
                var dBoy = await _apiDbContext.DeliveryBoy
                    .AsNoTracking()
                    .Where(x => x.Id == delivery.DeliveryBoyId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();
                sm.DeliveryBoyName = dBoy;
            }

            if (dm.PreparationTimeInMinutes > 0 && dm.SellerAcceptedAt.HasValue)
            {
                var deadline = dm.SellerAcceptedAt.Value.AddMinutes(dm.PreparationTimeInMinutes);
                var now = DateTime.UtcNow;

                var isPickedUp = false;
                if (delivery != null)
                {
                    isPickedUp = await _apiDbContext.DeliveryStatusHistory
                        .AsNoTracking()
                        .AnyAsync(h => h.DeliveryId == delivery.Id
                            && h.Status == DomainModels.Enums.DeliveryStatusDM.PickedUp);
                }

                if (isPickedUp)
                {
                    sm.PreparationStatus = "PickedUp";
                    sm.PreparationStatusMessage = "Order picked up by delivery partner!";
                }
                else if (now < deadline)
                {
                    sm.PreparationStatus = "Preparing";
                    var remaining = (int)(deadline - now).TotalMinutes;
                    sm.PreparationStatusMessage = $"Your order is being prepared. Estimated {remaining} min remaining.";
                }
                else
                {
                    sm.PreparationStatus = "DeliveryBoyLate";
                    sm.PreparationStatusMessage = "Delivery boy may be running late due to some issues.";
                }
            }

            return sm;
        }

        public async Task<OrderItemSM> GetOrderItemSM(OrderItemDM dm)
        {
            var order = await _apiDbContext.Order.FindAsync(dm.OrderId);
            var user = _apiDbContext.User.AsNoTracking().Where(x => x.Id == order.UserId).FirstOrDefault();

            string displayName = !string.IsNullOrWhiteSpace(user?.Name)
                ? user.Name
                : !string.IsNullOrWhiteSpace(user?.Username)
                    ? user.Username
                    : user?.Mobile;
            var variant = await _apiDbContext.ProductVariant.AsNoTracking().Include(v => v.Images).Where(x=>x.Id == dm.ProductVariantId).FirstOrDefaultAsync();
            var sm = _mapper.Map<OrderItemSM>(dm);
            var imgPath = !string.IsNullOrEmpty(variant?.Image)
                ? variant.Image
                : (variant?.Images != null && variant.Images.Any()
                    ? variant.Images.OrderBy(i => i.Id).First().Image
                    : null);
            if(!string.IsNullOrEmpty(imgPath))
            {
                var oImg = await _imageProcess.ResolveImage(imgPath);
                sm.ProductImage = oImg.Base64;
                sm.NetworkProductImage = oImg.NetworkUrl;
            }
            sm.CustomerName = displayName;
            sm.ProductName = variant?.Name;
            sm.OrderStatus = (OrderStatusSM)order.OrderStatus;
            sm.PaymentStatus = (PaymentStatusSM)order.PaymentStatus;
            sm.PaymentMode = (PaymentModeSM)order.PaymentMode;
            sm.IsAvailable = variant != null && variant.Status == ProductStatusDM.Active;

            // Deserialize toppings & addons from DB JSON strings back to lists
            if (!string.IsNullOrEmpty(dm.SelectedToppings))
            {
                try { sm.SelectedToppings = JsonSerializer.Deserialize<List<SelectedToppingItem>>(dm.SelectedToppings, _jsonOptions); }
                catch { sm.SelectedToppings = null; }
            }
            if (!string.IsNullOrEmpty(dm.SelectedAddons))
            {
                try { sm.SelectedAddons = JsonSerializer.Deserialize<List<SelectedAddonItem>>(dm.SelectedAddons, _jsonOptions); }
                catch { sm.SelectedAddons = null; }
            }

            return sm;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<OrderItemDetailSM> GetOrderItemDetailSM(OrderItemDM dm)
        {
            var order = await _apiDbContext.Order.FindAsync(dm.OrderId);
            var user = _apiDbContext.User.AsNoTracking().Where(x => x.Id == order.UserId).FirstOrDefault();

            string displayName = !string.IsNullOrWhiteSpace(user?.Name)
                ? user.Name
                : !string.IsNullOrWhiteSpace(user?.Username)
                    ? user.Username
                    : user?.Mobile;

            var variant = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .Include(v => v.Product)
                .Include(v => v.Images)
                .Where(x => x.Id == dm.ProductVariantId)
                .FirstOrDefaultAsync();

            string imageBase64 = null;
            string networkVariantImage = null;
            var imagePath = !string.IsNullOrEmpty(variant?.Image)
                ? variant.Image
                : (variant?.Images != null && variant.Images.Any()
                    ? variant.Images.OrderBy(i => i.Id).First().Image
                    : null);
            if (!string.IsNullOrEmpty(imagePath))
            {
                var oImg = await _imageProcess.ResolveImage(imagePath);
                imageBase64 = oImg.Base64;
                networkVariantImage = oImg.NetworkUrl;
            }

            // Parse toppings
            List<OrderToppingDetailSM>? toppings = null;
            if (!string.IsNullOrEmpty(dm.SelectedToppings))
            {
                try
                {
                    toppings = JsonSerializer.Deserialize<List<OrderToppingDetailSM>>(dm.SelectedToppings, _jsonOptions);
                }
                catch { toppings = null; }
            }

            // Parse addons
            List<OrderAddonDetailSM>? addons = null;
            if (!string.IsNullOrEmpty(dm.SelectedAddons))
            {
                try
                {
                    addons = JsonSerializer.Deserialize<List<OrderAddonDetailSM>>(dm.SelectedAddons, _jsonOptions);
                    // Enrich addon names and images with base product info for display
                    if (addons != null && addons.Any())
                    {
                        var addonIds = addons.Select(a => a.AddonProductId).Distinct().ToList();
                        var addonVariants = await _apiDbContext.ProductVariant
                            .AsNoTracking()
                            .Include(pv => pv.Product)
                            .Include(pv => pv.Images)
                            .Where(pv => addonIds.Contains(pv.Id))
                            .ToDictionaryAsync(pv => pv.Id);
                        foreach (var a in addons)
                        {
                            if (addonVariants.TryGetValue(a.AddonProductId, out var pv))
                            {
                                var baseName = pv.Product?.Name;
                                var varName = pv.Name;
                                if (!string.IsNullOrEmpty(baseName) && !string.IsNullOrEmpty(varName) && baseName != varName)
                                    a.AddonName = $"{baseName} - {varName}";
                                else if (!string.IsNullOrEmpty(baseName))
                                    a.AddonName = baseName;

                                // Resolve addon image
                                var addonImgPath = !string.IsNullOrEmpty(pv.Image)
                                    ? pv.Image
                                    : (pv.Images != null && pv.Images.Any() ? pv.Images.First().Image : null);
                                if (!string.IsNullOrEmpty(addonImgPath))
                                {
                                    var addonImg = await _imageProcess.ResolveImage(addonImgPath);
                                    a.AddonImage = addonImg.NetworkUrl ?? addonImg.Base64;
                                }
                            }
                        }
                    }
                }
                catch { addons = null; }
            }

            return new OrderItemDetailSM
            {
                Id = dm.Id,
                OrderId = dm.OrderId,
                CustomerName = displayName,
                ProductId = variant?.ProductId ?? 0,
                BaseProductName = variant?.Product?.Name,
                ProductVariantId = dm.ProductVariantId,
                VariantName = variant?.Name,
                VariantImageBase64 = imageBase64,
                NetworkVariantImage = networkVariantImage,
                Indicator = variant?.Indicator.ToString(),
                Quantity = dm.Quantity,
                UnitPrice = dm.UnitPrice,
                TotalPrice = dm.TotalPrice,
                OrderStatus = (OrderStatusSM)order.OrderStatus,
                PaymentStatus = (PaymentStatusSM)order.PaymentStatus,
                PaymentMode = (PaymentModeSM)order.PaymentMode,
                Toppings = toppings,
                Addons = addons,
                IsAvailable = variant != null && variant.Status == ProductStatusDM.Active
            };
        }

        public async Task<List<OrderItemDetailSM>> GetOrderItemsDetailAsync(
            long userId,
            long orderId,
            bool isSuperAdmin,
            int skip,
            int top)
        {
            var query = _apiDbContext.Order.AsNoTracking().AsQueryable();
            if (!isSuperAdmin)
            {
                query = query.Where(q => q.UserId == userId);
            }
            var order = await query.FirstOrDefaultAsync(x => x.Id == orderId);
            if (order == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log, $"User tried to access order with different user or order not found for Id: {orderId}",
                    "Order not found");
            }
            var orderItems = await _apiDbContext.OrderItem.Where(x => x.OrderId == orderId)
                .Skip(skip).Take(top)
                .ToListAsync();
            var result = new List<OrderItemDetailSM>();
            foreach (var item in orderItems)
            {
                result.Add(await GetOrderItemDetailSM(item));
            }
            return result;
        }

        #endregion Get OrderSM

        #region Get Delivery Charges

        public async Task<DeliveryChargeResponseSM> GetDeliveryCharges(long userId)
        {
            var defualtAddress = await _userAddressProcess.GetDefaultAddress(userId);
            if(defualtAddress == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"User with Id: {userId} has no default address",
                    "Please update the default address. Address is required for order");
            }
            var deliveryCharges = await _apiDbContext.DeliveryPlaces
                .AsNoTracking()
                .Where(x => x.Status == StatusDM.Active)
                .Select(x => x.DeliveryCharges)
                .FirstOrDefaultAsync();
            return new DeliveryChargeResponseSM()
            {
                DeliveryCharge = deliveryCharges
            };
        }

        #endregion Get Delivery Charges

        #region Invoice

        public async Task<OrderInvoiceSM> GenerateInvoice(long orderId, long userId, bool isSuperAdmin)
        {
            var query = _apiDbContext.Order.AsNoTracking().AsQueryable();
            if (!isSuperAdmin)
            {
                query = query.Where(o => o.UserId == userId);
            }
            var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.Fatal_Log,
                    $"Order not found for Id: {orderId}",
                    "Order not found");
            }

            // Seller
            InvoiceSellerSM sellerInfo = null;
            if (order.SellerId.HasValue && order.SellerId.Value > 0)
            {
                var seller = await _apiDbContext.Seller.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == order.SellerId.Value);
                if (seller != null)
                {
                    sellerInfo = new InvoiceSellerSM
                    {
                        Id = seller.Id,
                        Name = seller.Name,
                        StoreName = seller.StoreName,
                        Email = seller.Email,
                        Mobile = seller.Mobile,
                        Address = seller.FormattedAddress ?? seller.Street,
                        City = seller.City,
                        State = seller.State,
                        Country = seller.Country,
                        FssaiLicNo = seller.FssaiLicNo,
                        TaxName = seller.TaxName,
                        TaxNumber = seller.TaxNumber
                    };
                }
            }

            // Customer
            var user = await _apiDbContext.User.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == order.UserId);
            var customerInfo = new InvoiceCustomerSM
            {
                Id = user?.Id ?? 0,
                Name = !string.IsNullOrWhiteSpace(user?.Name) ? user.Name
                     : !string.IsNullOrWhiteSpace(user?.Username) ? user.Username
                     : user?.Mobile,
                Mobile = user?.Mobile,
                Email = user?.Email
            };

            // Delivery address
            InvoiceAddressSM addressInfo = null;
            if (order.AddressId.HasValue && order.AddressId.Value > 0)
            {
                var addr = await _userAddressProcess.GetById(order.AddressId.Value);
                if (addr != null)
                {
                    addressInfo = new InvoiceAddressSM
                    {
                        Name = addr.Name,
                        Mobile = addr.Mobile,
                        Address = addr.Address,
                        Landmark = addr.Landmark,
                        Area = addr.Area,
                        Pincode = addr.Pincode,
                        City = addr.City,
                        State = addr.State,
                        Country = addr.Country
                    };
                }
            }

            // Order items
            var orderItems = await _apiDbContext.OrderItem
                .Where(x => x.OrderId == orderId)
                .ToListAsync();

            var invoiceItems = new List<InvoiceItemSM>();
            decimal subtotal = 0;

            foreach (var item in orderItems)
            {
                var variant = await _apiDbContext.ProductVariant
                    .AsNoTracking()
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.Id == item.ProductVariantId);

                // Parse toppings
                List<OrderToppingDetailSM> toppings = null;
                decimal toppingsTotal = 0;
                if (!string.IsNullOrEmpty(item.SelectedToppings))
                {
                    try
                    {
                        toppings = JsonSerializer.Deserialize<List<OrderToppingDetailSM>>(item.SelectedToppings, _jsonOptions);
                        if (toppings != null)
                            toppingsTotal = toppings.Sum(t => t.Price * t.Quantity);
                    }
                    catch { }
                }

                // Parse addons
                List<OrderAddonDetailSM> addons = null;
                decimal addonsTotal = 0;
                if (!string.IsNullOrEmpty(item.SelectedAddons))
                {
                    try
                    {
                        addons = JsonSerializer.Deserialize<List<OrderAddonDetailSM>>(item.SelectedAddons, _jsonOptions);
                        if (addons != null)
                        {
                            addonsTotal = addons.Sum(a => a.Price * a.Quantity);
                            // Enrich addon names and images with base product info for display
                            var addonIds = addons.Select(a => a.AddonProductId).Distinct().ToList();
                            var addonVariants = await _apiDbContext.ProductVariant
                                .AsNoTracking()
                                .Include(pv => pv.Product)
                                .Include(pv => pv.Images)
                                .Where(pv => addonIds.Contains(pv.Id))
                                .ToDictionaryAsync(pv => pv.Id);
                            foreach (var a in addons)
                            {
                                if (addonVariants.TryGetValue(a.AddonProductId, out var pv))
                                {
                                    var baseName = pv.Product?.Name;
                                    var varName = pv.Name;
                                    if (!string.IsNullOrEmpty(baseName) && !string.IsNullOrEmpty(varName) && baseName != varName)
                                        a.AddonName = $"{baseName} - {varName}";
                                    else if (!string.IsNullOrEmpty(baseName))
                                        a.AddonName = baseName;

                                    var addonImgPath = !string.IsNullOrEmpty(pv.Image)
                                        ? pv.Image
                                        : (pv.Images != null && pv.Images.Any() ? pv.Images.First().Image : null);
                                    if (!string.IsNullOrEmpty(addonImgPath))
                                    {
                                        var addonImg = await _imageProcess.ResolveImage(addonImgPath);
                                        a.AddonImage = addonImg.NetworkUrl ?? addonImg.Base64;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                var lineTotal = item.TotalPrice + toppingsTotal + addonsTotal;
                subtotal += lineTotal;

                invoiceItems.Add(new InvoiceItemSM
                {
                    ProductVariantId = item.ProductVariantId,
                    BaseProductName = variant?.Product?.Name,
                    VariantName = variant?.Name,
                    Indicator = variant?.Indicator.ToString(),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    Toppings = toppings,
                    ToppingsTotal = toppingsTotal,
                    Addons = addons,
                    AddonsTotal = addonsTotal,
                    LineTotal = lineTotal
                });
            }

            return new OrderInvoiceSM
            {
                InvoiceNumber = $"INV-{order.OrderNumber}",
                InvoiceDate = order.CreatedAt ?? DateTime.UtcNow,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Seller = sellerInfo,
                Customer = customerInfo,
                DeliveryAddress = addressInfo,
                Items = invoiceItems,
                Subtotal = subtotal,
                DeliveryCharge = order.DeliveryCharge,
                PlatformCharge = order.PlatormCharge,
                CutleryCharge = order.CutlaryCharge,
                GiftWrapCharge = order.GiftWrapCharge,
                LowCartFeeCharge = order.LowCartFeeCharge,
                TipAmount = order.TipAmount,
                TotalAmount = order.Amount,
                Currency = order.Currency,
                PaymentMode = ((PaymentModeSM)order.PaymentMode).ToString(),
                PaymentStatus = ((PaymentStatusSM)order.PaymentStatus).ToString(),
                OrderStatus = ((OrderStatusSM)order.OrderStatus).ToString(),
                TransactionId = order.TransactionId,
                RazorpayPaymentId = order.RazorpayPaymentId
            };
        }

        #endregion Invoice

        #region Order Number Generator

        private static readonly char[] _orderNumberChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
        private static readonly Random _rng = new();

        private async Task<string> GenerateUniqueOrderNumber()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var chars = new char[8];
                lock (_rng)
                {
                    for (int i = 0; i < 8; i++)
                        chars[i] = _orderNumberChars[_rng.Next(_orderNumberChars.Length)];
                }
                var code = new string(chars);
                var exists = await _apiDbContext.Order.AnyAsync(o => o.OrderNumber == code);
                if (!exists) return code;
            }
            // Fallback: timestamp-based
            return DateTime.UtcNow.ToString("yyMMddHH") + Guid.NewGuid().ToString("N")[..4].ToUpper();
        }

        #endregion Order Number Generator

        #region Transaction Id Generator

        private async Task<long> GenerateUniqueTransactionId()
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                // 13-digit numeric: timestamp prefix (7) + random suffix (6)
                var tsPart = DateTime.UtcNow.Ticks % 10_000_000;
                long randomPart;
                lock (_rng)
                {
                    randomPart = _rng.Next(100_000, 999_999);
                }
                var txnId = tsPart * 1_000_000 + randomPart;
                var exists = await _apiDbContext.Order.AnyAsync(o => o.TransactionId == txnId);
                if (!exists) return txnId;
            }
            // Fallback: full ticks-based (guaranteed unique in practice)
            return Math.Abs(DateTime.UtcNow.Ticks + Environment.TickCount64);
        }

        #endregion Transaction Id Generator

        #region Seller Earnings

        public async Task<SellerEarningsSM> GetSellerEarningsAsync(long sellerId)
        {
            var seller = await _apiDbContext.Seller.FindAsync(sellerId);
            var commissionRate = seller?.Commission ?? 0;

            var today = IstDateHelper.IstDayStartUtc();
            var yesterday = IstDateHelper.IstDayStartUtc(IstDateHelper.Today.AddDays(-1));
            var weekStart = IstDateHelper.IstDayStartUtc(IstDateHelper.Today.AddDays(-7));
            var monthStart = IstDateHelper.IstMonthStartUtc();

            var baseItems = _apiDbContext.OrderItem
                .Where(x =>
                    x.ProductVariant.Product.SellerId == sellerId &&
                    x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                    x.Order.OrderStatus == OrderStatusDM.Delivered);

            var totalEarnings = await baseItems.SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var totalOrders = await baseItems.Select(x => x.OrderId).Distinct().CountAsync();

            var todayEarnings = await baseItems.Where(x => x.Order.CreatedAt >= today).SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var todayOrders = await baseItems.Where(x => x.Order.CreatedAt >= today).Select(x => x.OrderId).Distinct().CountAsync();

            var yesterdayEarnings = await baseItems.Where(x => x.Order.CreatedAt >= yesterday && x.Order.CreatedAt < today).SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var yesterdayOrders = await baseItems.Where(x => x.Order.CreatedAt >= yesterday && x.Order.CreatedAt < today).Select(x => x.OrderId).Distinct().CountAsync();

            var weekEarnings = await baseItems.Where(x => x.Order.CreatedAt >= weekStart).SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var weekOrders = await baseItems.Where(x => x.Order.CreatedAt >= weekStart).Select(x => x.OrderId).Distinct().CountAsync();

            var monthEarnings = await baseItems.Where(x => x.Order.CreatedAt >= monthStart).SumAsync(x => (decimal?)x.TotalPrice) ?? 0;
            var monthOrders = await baseItems.Where(x => x.Order.CreatedAt >= monthStart).Select(x => x.OrderId).Distinct().CountAsync();

            var totalCommission = totalEarnings * commissionRate / 100;

            return new SellerEarningsSM
            {
                TotalEarnings = totalEarnings - totalCommission,
                TodayEarnings = todayEarnings,
                YesterdayEarnings = yesterdayEarnings,
                WeekEarnings = weekEarnings,
                MonthEarnings = monthEarnings,
                TotalOrders = totalOrders,
                TodayOrders = todayOrders,
                YesterdayOrders = yesterdayOrders,
                WeekOrders = weekOrders,
                MonthOrders = monthOrders,
                CommissionRate = commissionRate,
                TotalCommission = totalCommission
            };
        }

        #endregion Seller Earnings

        #region Stock Helpers

        private async Task RestoreStockForOrder(long orderId)
        {
            var orderItems = await _apiDbContext.OrderItem
                .Where(x => x.OrderId == orderId)
                .ToListAsync();

            var variantIds = orderItems.Select(x => x.ProductVariantId).Distinct().ToList();
            var variants = await _apiDbContext.ProductVariant
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            foreach (var item in orderItems)
            {
                if (variants.TryGetValue(item.ProductVariantId, out var variant) && variant.Stock.HasValue)
                {
                    variant.Stock += item.Quantity;
                    variant.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        #endregion Stock Helpers

        #region Excel Export

        public async Task<byte[]> ExportOrdersToExcel(
            long? id,
            OrderStatusSM? orderStatus,
            PaymentStatusSM? paymentStatus,
            PaymentModeSM? paymentMode,
            long? sellerId,
            string? search,
            DateTime? dateFrom,
            DateTime? dateTo,
            decimal? minAmount,
            decimal? maxAmount,
            long? forceSellerId = null)
        {
            var query = BuildAdvancedQuery(id, orderStatus, paymentStatus, paymentMode,
                sellerId ?? forceSellerId, search, dateFrom, dateTo, minAmount, maxAmount);

            var orders = await query
                .OrderByDescending(x => x.CreatedAt)
                .Take(5000)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Orders");

            // Headers
            var headers = new[]
            {
                "Order #", "Order ID", "Date", "Customer", "Phone",
                "Amount", "Delivery Charge", "Platform Charge", "Tax", "Tip", "Total",
                "Payment Mode", "Payment Status", "Order Status",
                "Items",
                "Delivery Address", "City", "Pincode",
                "Delivery Boy", "Delivery Boy Phone", "Delivery Status",
                "Assigned At", "Delivered At",
                "Seller"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
            }

            // Style header row
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e293b");
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int row = 2;
            foreach (var order in orders)
            {
                // Customer
                var user = await _apiDbContext.User.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == order.UserId);

                // Items
                var items = await _apiDbContext.OrderItem.AsNoTracking()
                    .Where(oi => oi.OrderId == order.Id)
                    .Include(oi => oi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .ToListAsync();
                var itemsStr = string.Join(", ", items.Select(i =>
                    $"{i.ProductVariant?.Product?.Name ?? "Item"} x{i.Quantity} (₹{i.TotalPrice})"));

                // Address
                string addressStr = "", city = "", pincode = "";
                if (order.AddressId.HasValue && order.AddressId.Value > 0)
                {
                    var addr = await _userAddressProcess.GetById(order.AddressId.Value);
                    if (addr != null)
                    {
                        addressStr = $"{addr.Name}, {addr.Address}".Trim(',', ' ');
                        if (!string.IsNullOrEmpty(addr.Landmark))
                            addressStr += $", {addr.Landmark}";
                        city = addr.City ?? "";
                        pincode = addr.Pincode ?? "";
                    }
                }

                // Delivery boy
                string dBoyName = "", dBoyPhone = "", deliveryStatus = "", assignedAt = "", deliveredAt = "";
                var delivery = await _apiDbContext.Deliveries.AsNoTracking()
                    .Include(d => d.DeliveryBoy)
                    .Where(d => d.OrderId == order.Id)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();
                if (delivery != null)
                {
                    dBoyName = delivery.DeliveryBoy?.Name ?? "";
                    dBoyPhone = delivery.DeliveryBoy?.Mobile ?? "";
                    deliveryStatus = delivery.Status.ToString();
                    assignedAt = delivery.AssignedAt.ToString("yyyy-MM-dd HH:mm");
                    deliveredAt = delivery.DeliveredAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
                }

                // Seller
                string sellerName = "";
                if (order.SellerId.HasValue && order.SellerId.Value > 0)
                {
                    var seller = await _apiDbContext.Seller.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == order.SellerId.Value);
                    sellerName = seller?.StoreName ?? seller?.Name ?? "";
                }

                ws.Cell(row, 1).Value = order.OrderNumber;
                ws.Cell(row, 2).Value = order.Id;
                ws.Cell(row, 3).Value = order.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
                ws.Cell(row, 4).Value = user?.Name ?? user?.Username ?? $"User {order.UserId}";
                ws.Cell(row, 5).Value = user?.Mobile ?? "";
                ws.Cell(row, 6).Value = order.Amount - order.DeliveryCharge - order.PlatormCharge - order.TaxAmount - order.TipAmount;
                ws.Cell(row, 7).Value = order.DeliveryCharge;
                ws.Cell(row, 8).Value = order.PlatormCharge;
                ws.Cell(row, 9).Value = order.TaxAmount;
                ws.Cell(row, 10).Value = order.TipAmount;
                ws.Cell(row, 11).Value = order.Amount;
                ws.Cell(row, 12).Value = order.PaymentMode.ToString();
                ws.Cell(row, 13).Value = order.PaymentStatus.ToString();
                ws.Cell(row, 14).Value = order.OrderStatus.ToString();
                ws.Cell(row, 15).Value = itemsStr;
                ws.Cell(row, 16).Value = addressStr;
                ws.Cell(row, 17).Value = city;
                ws.Cell(row, 18).Value = pincode;
                ws.Cell(row, 19).Value = dBoyName;
                ws.Cell(row, 20).Value = dBoyPhone;
                ws.Cell(row, 21).Value = deliveryStatus;
                ws.Cell(row, 22).Value = assignedAt;
                ws.Cell(row, 23).Value = deliveredAt;
                ws.Cell(row, 24).Value = sellerName;

                // Alternate row shading
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                }

                row++;
            }

            // Auto-fit columns
            ws.Columns().AdjustToContents();
            // Cap max column width at 50
            foreach (var col in ws.ColumnsUsed())
            {
                if (col.Width > 50) col.Width = 50;
            }

            // Freeze header row
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        public async Task<byte[]> ExportSellerOrdersToExcel(
            long sellerId,
            string? tab,
            DateTime? dateFrom,
            DateTime? dateTo)
        {
            OrderStatusSM? orderStatus = null;
            if (tab == "pending") orderStatus = OrderStatusSM.Created;
            else if (tab == "accepted") orderStatus = OrderStatusSM.SellerAccepted;
            else if (tab == "completed") orderStatus = OrderStatusSM.Delivered;

            return await ExportOrdersToExcel(
                id: null,
                orderStatus: orderStatus,
                paymentStatus: null,
                paymentMode: null,
                sellerId: null,
                search: null,
                dateFrom: dateFrom,
                dateTo: dateTo,
                minAmount: null,
                maxAmount: null,
                forceSellerId: sellerId);
        }

        #endregion Excel Export
    }
}
