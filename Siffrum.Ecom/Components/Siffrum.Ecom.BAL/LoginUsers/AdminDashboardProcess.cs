using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.ServiceModels.v1;
using Siffrum.Ecom.ServiceModels.v1.Dashboard.AdminDashboard;

namespace Siffrum.Ecom.BAL.LoginUsers
{
    public class AdminDashboardProcess : SiffrumBalBase
    {

        public AdminDashboardProcess(IMapper mapper,ApiDbContext apiDbContext)
        : base(mapper, apiDbContext)
        {
        }

        public async Task<AdminDashboardResponseSM> GetDashboardAsync(DateTime? date = null)
        {
            var istToday = IstDateHelper.Today;
            var selectedDay = date?.Date ?? istToday;
            var dayStart = IstDateHelper.IstDayStartUtc(selectedDay);
            var dayEnd = IstDateHelper.IstDayEndUtc(selectedDay);
            var firstDayOfMonth = IstDateHelper.IstDayStartUtc(new DateTime(selectedDay.Year, selectedDay.Month, 1));
            var weekStart = IstDateHelper.IstDayStartUtc(selectedDay.AddDays(-7));

            var response = new AdminDashboardResponseSM();

            #region PLATFORM KPIs

            try
            {
                var monthlyOrders = await _apiDbContext.Order
                    .Where(o => o.CreatedAt >= firstDayOfMonth
                        && o.PaymentStatus == PaymentStatusDM.Paid
                        && o.OrderStatus == OrderStatusDM.Delivered)
                    .ToListAsync();

                response.PlatformKpis.GmvThisMonth = monthlyOrders.Sum(x => x.Amount);

                try
                {
                    response.PlatformKpis.PlatformCommissionEarned =
                        await _apiDbContext.OrderItem
                            .Where(x => x.Order.CreatedAt >= firstDayOfMonth &&
                                        x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                                        x.Order.OrderStatus == OrderStatusDM.Delivered &&
                                        x.ProductVariant != null &&
                                        x.ProductVariant.Product != null &&
                                        x.ProductVariant.Product.Seller != null)
                            .SumAsync(x =>
                                (decimal?)((x.UnitPrice * x.Quantity) *
                                (x.ProductVariant.Product.Seller.Commission / 100))) ?? 0;
                }
                catch { response.PlatformKpis.PlatformCommissionEarned = 0; }

                response.PlatformKpis.TotalOrdersToday =
                    await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd
                            && x.OrderStatus != OrderStatusDM.AwaitingPayment)
                        .CountAsync();

                response.PlatformKpis.ActiveVendors =
                    await _apiDbContext.Seller
                        .Where(x => x.Status == SellerStatusDM.Active && x.DeletedAt == null)
                        .CountAsync();

                response.PlatformKpis.NewVendorsThisWeek =
                    await _apiDbContext.Seller
                        .Where(x => x.CreatedAt >= weekStart)
                        .CountAsync();

                response.PlatformKpis.PendingRefundRequests =
                    await _apiDbContext.Order
                        .Where(x => x.PaymentStatus == PaymentStatusDM.RefundInitiated
                            || (x.PaymentStatus == PaymentStatusDM.Paid
                                && (x.OrderStatus == OrderStatusDM.Returned || x.OrderStatus == OrderStatusDM.Cancelled || x.OrderStatus == OrderStatusDM.CancelledBySeller)))
                        .CountAsync();
            }
            catch { }

            #endregion

            #region REVENUE ANALYTICS (30 DAYS)

            try
            {
                var istOffset = TimeSpan.FromHours(5.5);
                var last30Days = IstDateHelper.IstDayStartUtc(istToday.AddDays(-30));

                var rawRevenue = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last30Days
                        && x.PaymentStatus == PaymentStatusDM.Paid
                        && x.OrderStatus == OrderStatusDM.Delivered)
                    .Select(x => new { x.Amount, x.CreatedAt })
                    .ToListAsync();

                var revenueByIstDay = rawRevenue
                    .GroupBy(x => (x.CreatedAt ?? DateTime.UtcNow).Add(istOffset).Date)
                    .ToDictionary(
                        g => g.Key,
                        g => new DailyRevenueSM
                        {
                            Date = g.Key,
                            Amount = (double)g.Sum(i => i.Amount),
                            OrderCount = g.Count()
                        });

                var filledRevenue = new List<DailyRevenueSM>();
                for (int i = -30; i <= 0; i++)
                {
                    var day = istToday.AddDays(i);
                    filledRevenue.Add(revenueByIstDay.TryGetValue(day, out var pt)
                        ? pt
                        : new DailyRevenueSM { Date = day, Amount = 0, OrderCount = 0 });
                }
                response.RevenueAnalytics.CommissionTrend = filledRevenue;

                var rawTotals = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last30Days)
                    .Select(x => new { x.OrderStatus, x.CreatedAt })
                    .ToListAsync();

                var totalsByIstDay = rawTotals
                    .GroupBy(x => (x.CreatedAt ?? DateTime.UtcNow).Add(istOffset).Date)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            TotalOrders = g.Count(),
                            RefundedOrders = g.Count(o => o.OrderStatus == OrderStatusDM.Returned || o.OrderStatus == OrderStatusDM.Cancelled)
                        });

                var filledRefund = new List<RefundRatioSM>();
                for (int i = -30; i <= 0; i++)
                {
                    var day = istToday.AddDays(i);
                    if (totalsByIstDay.TryGetValue(day, out var d))
                        filledRefund.Add(new RefundRatioSM
                        {
                            Date = day,
                            RefundRatioPercentage = d.TotalOrders > 0
                                ? Math.Round((double)d.RefundedOrders / d.TotalOrders * 100, 2)
                                : 0
                        });
                    else
                        filledRefund.Add(new RefundRatioSM { Date = day, RefundRatioPercentage = 0 });
                }
                response.RevenueAnalytics.RefundRatioOverlay = filledRefund;
            }
            catch { }

            #endregion

            #region VENDOR HEALTH

            try
            {
                var allVendorStats = await _apiDbContext.OrderItem
                    .Where(x => x.Order.CreatedAt >= firstDayOfMonth &&
                                x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                                x.Order.OrderStatus == OrderStatusDM.Delivered &&
                                x.ProductVariant != null &&
                                x.ProductVariant.Product != null &&
                                x.ProductVariant.Product.Seller != null)
                    .GroupBy(x => new
                    {
                        x.ProductVariant.Product.SellerId,
                        x.ProductVariant.Product.Seller.StoreName
                    })
                    .Select(g => new TopVendorSM
                    {
                        SellerId = g.Key.SellerId,
                        StoreName = g.Key.StoreName,
                        TotalSales = g.Sum(x => x.UnitPrice * x.Quantity)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .ToListAsync();

                response.VendorHealth.TopVendorsBySales = allVendorStats.Take(5).ToList();

                var vendorRefundStats = await _apiDbContext.OrderItem
                    .Where(x => x.Order.CreatedAt >= firstDayOfMonth &&
                                x.ProductVariant != null &&
                                x.ProductVariant.Product != null &&
                                x.ProductVariant.Product.Seller != null)
                    .GroupBy(x => new
                    {
                        x.ProductVariant.Product.SellerId,
                        x.ProductVariant.Product.Seller.StoreName
                    })
                    .Select(g => new
                    {
                        SellerId = g.Key.SellerId,
                        StoreName = g.Key.StoreName,
                        TotalOrders = g.Select(x => x.OrderId).Distinct().Count(),
                        RefundedOrders = g.Where(x =>
                            x.Order.OrderStatus == OrderStatusDM.Returned ||
                            x.Order.OrderStatus == OrderStatusDM.Cancelled)
                            .Select(x => x.OrderId).Distinct().Count(),
                        TotalSales = g.Sum(x => x.UnitPrice * x.Quantity)
                    })
                    .ToListAsync();

                response.VendorHealth.VendorsWithHighRefundRate = vendorRefundStats
                    .Where(v => v.TotalOrders > 0 && ((double)v.RefundedOrders / v.TotalOrders * 100) > 10)
                    .Select(v => new TopVendorSM
                    {
                        SellerId = v.SellerId,
                        StoreName = v.StoreName,
                        TotalSales = v.TotalSales,
                        RefundRate = Math.Round((double)v.RefundedOrders / v.TotalOrders * 100, 1)
                    })
                    .OrderByDescending(x => x.RefundRate)
                    .Take(5)
                    .ToList();

                response.VendorHealth.DeactivatedSellers =
                    await _apiDbContext.Seller
                        .Where(x => x.Status == SellerStatusDM.Deactivated)
                        .CountAsync();
            }
            catch { }

            #endregion

            #region ORDER HEALTH

            try
            {
                response.OrderHealth.OrdersByStatus =
                    await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd)
                        .GroupBy(x => x.OrderStatus)
                        .Select(g => new OrderStatusCountSM
                        {
                            Status = g.Key.ToString(),
                            Count = g.Count()
                        }).ToListAsync();
            }
            catch { }

            try
            {
                response.OrderHealth.InQueueOrders =
                    await _apiDbContext.Order
                        .Where(x => x.OrderStatus == OrderStatusDM.SellerAccepted
                            || x.OrderStatus == OrderStatusDM.Processing
                            || x.OrderStatus == OrderStatusDM.Shipped
                            || x.OrderStatus == OrderStatusDM.Assigned
                            || x.OrderStatus == OrderStatusDM.PickedUp
                            || x.OrderStatus == OrderStatusDM.OutForDelivery)
                        .CountAsync();
            }
            catch { }

            try
            {
                response.OrderHealth.FailedPayments =
                    await _apiDbContext.Order
                        .Where(x => x.PaymentStatus == PaymentStatusDM.Failed)
                        .CountAsync();

                response.OrderHealth.PendingPayments =
                    await _apiDbContext.Order
                        .Where(x => x.PaymentStatus == PaymentStatusDM.Pending)
                        .CountAsync();
            }
            catch { }

            #endregion

            #region PAYMENT MODE DISTRIBUTION

            try
            {
                var paymentModes = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= firstDayOfMonth)
                    .GroupBy(x => x.PaymentMode)
                    .Select(g => new PaymentModeCountSM
                    {
                        Mode = g.Key.ToString(),
                        Count = g.Count(),
                        Amount = g.Sum(x => x.Amount)
                    })
                    .ToListAsync();

                response.PaymentModeDistribution = paymentModes;
            }
            catch { }

            #endregion

            #region HOURLY ORDER DISTRIBUTION (last 7 days)

            try
            {
                var last7Days = IstDateHelper.IstDayStartUtc(istToday.AddDays(-7));

                var hourlyOrders = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last7Days)
                    .GroupBy(x => x.CreatedAt.Value.Hour)
                    .Select(g => new HourlyOrderSM
                    {
                        Hour = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Hour)
                    .ToListAsync();

                response.HourlyOrderDistribution = hourlyOrders;
            }
            catch { }

            #endregion

            #region CATEGORY REVENUE (this month)

            try
            {
                var categoryRevenue = await _apiDbContext.OrderItem
                    .Where(x => x.Order.CreatedAt >= firstDayOfMonth &&
                                x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                                x.Order.OrderStatus == OrderStatusDM.Delivered &&
                                x.ProductVariant != null &&
                                x.ProductVariant.Product != null &&
                                x.ProductVariant.Product.Category != null)
                    .GroupBy(x => x.ProductVariant.Product.Category.Name)
                    .Select(g => new CategoryRevenueSM
                    {
                        Category = g.Key ?? "Uncategorized",
                        Revenue = g.Sum(x => x.TotalPrice),
                        OrderCount = g.Select(x => x.OrderId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToListAsync();

                response.CategoryRevenue = categoryRevenue;
            }
            catch { }

            #endregion

            return response;
        }

        public async Task<List<SellerCollectionSM>> GetSellerCollectionsAsync(DateTime? from = null, DateTime? to = null)
        {
            var today = IstDateHelper.IstDayStartUtc();
            var yesterday = IstDateHelper.IstDayStartUtc(IstDateHelper.Today.AddDays(-1));
            var monthStart = IstDateHelper.IstMonthStartUtc();

            var fromStart = from.HasValue
                ? IstDateHelper.IstDayStartUtc(from.Value.Date)
                : (DateTime?)null;
            var toEnd = to.HasValue
                ? IstDateHelper.IstDayEndUtc(to.Value.Date)
                : (DateTime?)null;

            var sellers = await _apiDbContext.Seller
                .Where(s => s.DeletedAt == null && s.Status == SellerStatusDM.Active)
                .ToListAsync();

            var result = new List<SellerCollectionSM>();

            foreach (var seller in sellers)
            {
                var sellerId = seller.Id;

                var totalOrders = await _apiDbContext.OrderItem
                    .Where(x =>
                        x.ProductVariant.Product.SellerId == sellerId &&
                        x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                        x.Order.OrderStatus == OrderStatusDM.Delivered)
                    .Select(x => x.OrderId)
                    .Distinct()
                    .CountAsync();

                var totalCollection = await _apiDbContext.OrderItem
                    .Where(x =>
                        x.ProductVariant.Product.SellerId == sellerId &&
                        x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                        x.Order.OrderStatus == OrderStatusDM.Delivered)
                    .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

                var yesterdayStart = yesterday.AddHours(-5).AddMinutes(-30);
                var yesterdayEnd = yesterdayStart.AddDays(1);
                var yesterdayCollection = await _apiDbContext.OrderItem
                    .Where(x =>
                        x.ProductVariant.Product.SellerId == sellerId &&
                        x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                        x.Order.OrderStatus == OrderStatusDM.Delivered &&
                        x.Order.CreatedAt >= yesterdayStart && x.Order.CreatedAt < yesterdayEnd)
                    .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

                var monthStartIst = monthStart.AddHours(-5).AddMinutes(-30);
                var monthCollection = await _apiDbContext.OrderItem
                    .Where(x =>
                        x.ProductVariant.Product.SellerId == sellerId &&
                        x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                        x.Order.OrderStatus == OrderStatusDM.Delivered &&
                        x.Order.CreatedAt >= monthStartIst)
                    .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

                decimal filteredCollection = 0;
                int filteredOrders = 0;
                if (fromStart.HasValue && toEnd.HasValue)
                {
                    filteredCollection = await _apiDbContext.OrderItem
                        .Where(x =>
                            x.ProductVariant.Product.SellerId == sellerId &&
                            x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                            x.Order.OrderStatus == OrderStatusDM.Delivered &&
                            x.Order.CreatedAt >= fromStart.Value && x.Order.CreatedAt < toEnd.Value)
                        .SumAsync(x => (decimal?)x.TotalPrice) ?? 0;

                    filteredOrders = await _apiDbContext.OrderItem
                        .Where(x =>
                            x.ProductVariant.Product.SellerId == sellerId &&
                            x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                            x.Order.OrderStatus == OrderStatusDM.Delivered &&
                            x.Order.CreatedAt >= fromStart.Value && x.Order.CreatedAt < toEnd.Value)
                        .Select(x => x.OrderId)
                        .Distinct()
                        .CountAsync();
                }

                result.Add(new SellerCollectionSM
                {
                    SellerId = sellerId,
                    StoreName = seller.StoreName ?? seller.Name ?? $"Seller #{sellerId}",
                    SellerName = seller.Name ?? "",
                    TotalCollection = totalCollection,
                    YesterdayCollection = yesterdayCollection,
                    FilteredCollection = filteredCollection,
                    MonthCollection = monthCollection,
                    TotalOrders = totalOrders,
                    FilteredOrders = filteredOrders
                });
            }

            return result.OrderByDescending(x => x.TotalCollection).ToList();
        }

        public async Task<List<SellerOrderSummarySM>> GetSellerOrderSummaryAsync(
            DateTime date, string statusFilter)
        {
            var istOffset = TimeSpan.FromHours(5.5);
            var isLiveStatus = statusFilter?.ToLower() == "inqueue";

            var query = _apiDbContext.Order.AsNoTracking()
                .Where(o => o.OrderStatus != OrderStatusDM.AwaitingPayment);

            if (!isLiveStatus)
            {
                var dayStart = IstDateHelper.IstDayStartUtc(date.Date);
                var dayEnd = IstDateHelper.IstDayEndUtc(date.Date);
                query = query.Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd);
            }

            query = ApplyStatusFilter(query, statusFilter);

            var raw = await query
                .Select(o => new { o.SellerId, o.Amount, o.CreatedAt })
                .ToListAsync();

            var grouped = raw
                .Where(o => o.SellerId.HasValue)
                .GroupBy(o => new
                {
                    SellerId = o.SellerId!.Value,
                    IstDate = isLiveStatus
                        ? (o.CreatedAt ?? DateTime.UtcNow).Add(istOffset).Date
                        : date.Date
                })
                .Select(g => new
                {
                    g.Key.SellerId,
                    g.Key.IstDate,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.Amount)
                })
                .ToList();

            var sellerIds = grouped.Select(g => g.SellerId).Distinct().ToList();
            var sellers = await _apiDbContext.Seller.AsNoTracking()
                .Where(s => sellerIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.StoreName ?? s.Name ?? $"Seller #{s.Id}");

            return grouped
                .Select(g => new SellerOrderSummarySM
                {
                    SellerId = g.SellerId,
                    StoreName = sellers.ContainsKey(g.SellerId) ? sellers[g.SellerId] : $"Seller #{g.SellerId}",
                    OrderCount = g.OrderCount,
                    Revenue = g.Revenue,
                    CreatedDate = isLiveStatus ? g.IstDate : null
                })
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.OrderCount)
                .ToList();
        }

        public async Task<List<OrderDrillDownItemSM>> GetSellerOrderDetailsAsync(
            long sellerId, DateTime date, string statusFilter)
        {
            var isLiveStatus = statusFilter?.ToLower() == "inqueue";

            var query = _apiDbContext.Order.AsNoTracking()
                .Where(o => o.SellerId == sellerId
                    && o.OrderStatus != OrderStatusDM.AwaitingPayment);

            if (!isLiveStatus)
            {
                var dayStart = IstDateHelper.IstDayStartUtc(date.Date);
                var dayEnd = IstDateHelper.IstDayEndUtc(date.Date);
                query = query.Where(o => o.CreatedAt >= dayStart && o.CreatedAt < dayEnd);
            }

            query = ApplyStatusFilter(query, statusFilter);

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderDrillDownItemSM
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber,
                    Amount = o.Amount,
                    OrderStatus = o.OrderStatus.ToString(),
                    PaymentStatus = o.PaymentStatus.ToString(),
                    PaymentMode = o.PaymentMode.ToString(),
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
        }

        private static IQueryable<DomainModels.v1.OrderDM> ApplyStatusFilter(
            IQueryable<DomainModels.v1.OrderDM> query, string statusFilter)
        {
            switch (statusFilter?.ToLower())
            {
                case "delivered":
                    return query.Where(o => o.OrderStatus == OrderStatusDM.Delivered);
                case "cancelled":
                    return query.Where(o => o.OrderStatus == OrderStatusDM.Cancelled
                        || o.OrderStatus == OrderStatusDM.CancelledBySeller);
                case "failed":
                    return query.Where(o => o.OrderStatus == OrderStatusDM.Failed);
                case "inqueue":
                    return query.Where(o => o.OrderStatus == OrderStatusDM.SellerAccepted
                        || o.OrderStatus == OrderStatusDM.Processing
                        || o.OrderStatus == OrderStatusDM.Shipped
                        || o.OrderStatus == OrderStatusDM.Assigned
                        || o.OrderStatus == OrderStatusDM.PickedUp
                        || o.OrderStatus == OrderStatusDM.OutForDelivery);
                case "pending_payments":
                    return query.Where(o => o.PaymentStatus == PaymentStatusDM.Pending);
                default:
                    return query;
            }
        }
    }
}
