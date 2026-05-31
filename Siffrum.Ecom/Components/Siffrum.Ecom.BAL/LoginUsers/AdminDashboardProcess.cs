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

        public async Task<AdminDashboardResponseSM> GetDashboardAsync(DateTime? date = null, int? platform = null)
        {
            var istToday = IstDateHelper.Today;
            var selectedDay = date?.Date ?? istToday;
            var dayStart = IstDateHelper.IstDayStartUtc(selectedDay);
            var dayEnd = IstDateHelper.IstDayEndUtc(selectedDay);
            var firstDayOfMonth = IstDateHelper.IstDayStartUtc(new DateTime(selectedDay.Year, selectedDay.Month, 1));
            var weekStart = IstDateHelper.IstDayStartUtc(selectedDay.AddDays(-7));

            // Optional platform filter: 1=HotBox, 2=SpeedyMart, null=all
            PlatformTypeDM? platformFilter = platform.HasValue ? (PlatformTypeDM)platform.Value : null;

            var response = new AdminDashboardResponseSM();

            #region PLATFORM KPIs

            try
            {
                var monthlyOrdersQuery = _apiDbContext.Order
                    .Where(o => o.CreatedAt >= firstDayOfMonth
                        && o.PaymentStatus == PaymentStatusDM.Paid
                        && o.OrderStatus == OrderStatusDM.Delivered);
                if (platformFilter.HasValue)
                    monthlyOrdersQuery = monthlyOrdersQuery.Where(o => o.PlatformType == platformFilter.Value);
                var monthlyOrders = await monthlyOrdersQuery.ToListAsync();

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

                var todayQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd
                        && x.OrderStatus != OrderStatusDM.AwaitingPayment);
                if (platformFilter.HasValue)
                    todayQuery = todayQuery.Where(x => x.PlatformType == platformFilter.Value);
                response.PlatformKpis.TotalOrdersToday = await todayQuery.CountAsync();

                response.PlatformKpis.ActiveVendors =
                    await _apiDbContext.Seller
                        .Where(x => x.Status == SellerStatusDM.Active && x.DeletedAt == null)
                        .CountAsync();

                response.PlatformKpis.NewVendorsThisWeek =
                    await _apiDbContext.Seller
                        .Where(x => x.CreatedAt >= weekStart)
                        .CountAsync();

                var refundQuery = _apiDbContext.Order
                    .Where(x => x.PaymentStatus == PaymentStatusDM.RefundInitiated
                        || (x.PaymentStatus == PaymentStatusDM.Paid
                            && (x.OrderStatus == OrderStatusDM.Returned || x.OrderStatus == OrderStatusDM.Cancelled || x.OrderStatus == OrderStatusDM.CancelledBySeller)));
                if (platformFilter.HasValue)
                    refundQuery = refundQuery.Where(x => x.PlatformType == platformFilter.Value);
                response.PlatformKpis.PendingRefundRequests = await refundQuery.CountAsync();
            }
            catch { }

            #endregion

            #region REVENUE ANALYTICS (30 DAYS)

            try
            {
                var istOffset = TimeSpan.FromHours(5.5);
                var last30Days = IstDateHelper.IstDayStartUtc(istToday.AddDays(-30));

                var revenueQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last30Days
                        && x.PaymentStatus == PaymentStatusDM.Paid
                        && x.OrderStatus == OrderStatusDM.Delivered);
                if (platformFilter.HasValue)
                    revenueQuery = revenueQuery.Where(x => x.PlatformType == platformFilter.Value);
                var rawRevenue = await revenueQuery
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

                var totalsQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last30Days);
                if (platformFilter.HasValue)
                    totalsQuery = totalsQuery.Where(x => x.PlatformType == platformFilter.Value);
                var rawTotals = await totalsQuery
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
                var orderStatusQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
                if (platformFilter.HasValue)
                    orderStatusQuery = orderStatusQuery.Where(x => x.PlatformType == platformFilter.Value);
                response.OrderHealth.OrdersByStatus = await orderStatusQuery
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
                var inQueueQuery = _apiDbContext.Order
                    .Where(x => x.OrderStatus == OrderStatusDM.SellerAccepted
                        || x.OrderStatus == OrderStatusDM.Processing
                        || x.OrderStatus == OrderStatusDM.Shipped
                        || x.OrderStatus == OrderStatusDM.Assigned
                        || x.OrderStatus == OrderStatusDM.PickedUp
                        || x.OrderStatus == OrderStatusDM.OutForDelivery);
                if (platformFilter.HasValue)
                    inQueueQuery = inQueueQuery.Where(x => x.PlatformType == platformFilter.Value);
                response.OrderHealth.InQueueOrders = await inQueueQuery.CountAsync();
            }
            catch { }

            try
            {
                var failedQuery = _apiDbContext.Order
                    .Where(x => x.PaymentStatus == PaymentStatusDM.Failed);
                if (platformFilter.HasValue)
                    failedQuery = failedQuery.Where(x => x.PlatformType == platformFilter.Value);
                response.OrderHealth.FailedPayments = await failedQuery.CountAsync();

                var pendingQuery = _apiDbContext.Order
                    .Where(x => x.PaymentStatus == PaymentStatusDM.Pending);
                if (platformFilter.HasValue)
                    pendingQuery = pendingQuery.Where(x => x.PlatformType == platformFilter.Value);
                response.OrderHealth.PendingPayments = await pendingQuery.CountAsync();
            }
            catch { }

            #endregion

            #region PAYMENT MODE DISTRIBUTION

            try
            {
                var payQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= firstDayOfMonth);
                if (platformFilter.HasValue)
                    payQuery = payQuery.Where(x => x.PlatformType == platformFilter.Value);
                var paymentModes = await payQuery
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

                var hourlyQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last7Days);
                if (platformFilter.HasValue)
                    hourlyQuery = hourlyQuery.Where(x => x.PlatformType == platformFilter.Value);
                var hourlyOrders = await hourlyQuery
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

            #region PERFORMANCE METRICS

            try
            {
                var perfQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= firstDayOfMonth);
                if (platformFilter.HasValue)
                    perfQuery = perfQuery.Where(x => x.PlatformType == platformFilter.Value);

                var allOrders = await perfQuery
                    .Select(x => new
                    {
                        x.OrderStatus,
                        x.PaymentStatus,
                        x.CreatedAt,
                        x.ExpectedDeliveryDate,
                        x.Amount
                    })
                    .ToListAsync();

                var totalOrdersCount = allOrders.Count;
                var deliveredOrders = allOrders.Where(x => x.OrderStatus == OrderStatusDM.Delivered).ToList();

                // Perfect Order Rate: Orders delivered on time, complete, and paid
                var perfectOrders = deliveredOrders.Count(x =>
                    x.PaymentStatus == PaymentStatusDM.Paid &&
                    (!x.ExpectedDeliveryDate.HasValue || x.CreatedAt <= x.ExpectedDeliveryDate.Value));
                response.PerformanceMetrics.PerfectOrderRate = totalOrdersCount > 0
                    ? Math.Round((double)perfectOrders / totalOrdersCount * 100, 2)
                    : 0;
                response.PerformanceMetrics.TotalOrders = totalOrdersCount;
                response.PerformanceMetrics.PerfectOrders = perfectOrders;

                // Fill Rate: Orders that were successfully delivered vs total orders
                response.PerformanceMetrics.FillRate = totalOrdersCount > 0
                    ? Math.Round((double)deliveredOrders.Count / totalOrdersCount * 100, 2)
                    : 0;

                // Delivered Accuracy: On-time deliveries / total deliveries
                var onTimeDeliveries = deliveredOrders.Count(x =>
                    !x.ExpectedDeliveryDate.HasValue || x.CreatedAt <= x.ExpectedDeliveryDate.Value);
                response.PerformanceMetrics.DeliveredAccuracyRate = deliveredOrders.Count > 0
                    ? Math.Round((double)onTimeDeliveries / deliveredOrders.Count * 100, 2)
                    : 0;
                response.PerformanceMetrics.OnTimeDeliveries = onTimeDeliveries;
                response.PerformanceMetrics.CompleteDeliveries = deliveredOrders.Count;
            }
            catch { }

            #endregion

            #region RETURN RATE ANALYTICS

            try
            {
                var returnQuery = _apiDbContext.Order
                    .Where(x => x.CreatedAt >= firstDayOfMonth);
                if (platformFilter.HasValue)
                    returnQuery = returnQuery.Where(x => x.PlatformType == platformFilter.Value);

                var allOrdersForReturns = await returnQuery
                    .Select(x => new { x.OrderStatus, x.PaymentStatus })
                    .ToListAsync();

                var totalOrdersForReturns = allOrdersForReturns.Count;
                var returnedOrders = allOrdersForReturns.Count(x => x.OrderStatus == OrderStatusDM.Returned);
                var refundedOrders = allOrdersForReturns.Count(x => x.PaymentStatus == PaymentStatusDM.RefundInitiated || x.PaymentStatus == PaymentStatusDM.Refunded);
                var cancelledOrders = allOrdersForReturns.Count(x => x.OrderStatus == OrderStatusDM.Cancelled || x.OrderStatus == OrderStatusDM.CancelledBySeller);

                response.ReturnRateAnalytics.TotalOrders = totalOrdersForReturns;
                response.ReturnRateAnalytics.ReturnedOrders = returnedOrders;
                response.ReturnRateAnalytics.RefundedOrders = refundedOrders;
                response.ReturnRateAnalytics.CancelledOrders = cancelledOrders;

                response.ReturnRateAnalytics.OverallReturnRate = totalOrdersForReturns > 0
                    ? Math.Round((double)returnedOrders / totalOrdersForReturns * 100, 2)
                    : 0;
                response.ReturnRateAnalytics.RefundRate = totalOrdersForReturns > 0
                    ? Math.Round((double)refundedOrders / totalOrdersForReturns * 100, 2)
                    : 0;
                response.ReturnRateAnalytics.CancellationRate = totalOrdersForReturns > 0
                    ? Math.Round((double)cancelledOrders / totalOrdersForReturns * 100, 2)
                    : 0;
            }
            catch { }

            #endregion

            #region CUSTOMER LTV

            try
            {
                var customerOrders = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= firstDayOfMonth &&
                                x.PaymentStatus == PaymentStatusDM.Paid &&
                                x.OrderStatus == OrderStatusDM.Delivered)
                    .GroupBy(x => x.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        TotalSpent = g.Sum(x => x.Amount),
                        OrderCount = g.Count()
                    })
                    .ToListAsync();

                var totalCustomers = customerOrders.Count;
                var totalLtv = customerOrders.Sum(x => x.TotalSpent);
                var avgLtv = totalCustomers > 0 ? totalLtv / totalCustomers : 0;

                response.CustomerLtv.TotalCustomers = totalCustomers;
                response.CustomerLtv.TotalLtv = (double)totalLtv;
                response.CustomerLtv.AverageLtv = (double)avgLtv;

                // Segment customers by LTV
                var segments = new List<CustomerLtvSegmentSM>();
                if (totalCustomers > 0)
                {
                    var low = customerOrders.Where(x => x.TotalSpent < 500).ToList();
                    var medium = customerOrders.Where(x => x.TotalSpent >= 500 && x.TotalSpent < 2000).ToList();
                    var high = customerOrders.Where(x => x.TotalSpent >= 2000).ToList();

                    segments.Add(new CustomerLtvSegmentSM
                    {
                        Segment = "Low (< ₹500)",
                        CustomerCount = low.Count,
                        AverageLtv = low.Count > 0 ? (double)low.Average(x => x.TotalSpent) : 0
                    });
                    segments.Add(new CustomerLtvSegmentSM
                    {
                        Segment = "Medium (₹500-₹2000)",
                        CustomerCount = medium.Count,
                        AverageLtv = medium.Count > 0 ? (double)medium.Average(x => x.TotalSpent) : 0
                    });
                    segments.Add(new CustomerLtvSegmentSM
                    {
                        Segment = "High (≥ ₹2000)",
                        CustomerCount = high.Count,
                        AverageLtv = high.Count > 0 ? (double)high.Average(x => x.TotalSpent) : 0
                    });
                }
                response.CustomerLtv.Segments = segments;
            }
            catch { }

            #endregion

            #region NPS METRICS

            try
            {
                // Use ProductRating for NPS calculation
                var ratings = await _apiDbContext.ProductRating
                    .Where(x => x.CreatedAt >= firstDayOfMonth && x.Rate > 0)
                    .Select(x => x.Rate)
                    .ToListAsync();

                var totalResponses = ratings.Count;
                if (totalResponses > 0)
                {
                    var promoters = ratings.Count(x => x >= 4);
                    var passives = ratings.Count(x => x == 3);
                    var detractors = ratings.Count(x => x <= 2);
                    var avgRating = ratings.Any() ? ratings.Average(x => x) : 0;

                    response.NpsMetrics.TotalResponses = totalResponses;
                    response.NpsMetrics.Promoters = promoters;
                    response.NpsMetrics.Passives = passives;
                    response.NpsMetrics.Detractors = detractors;
                    response.NpsMetrics.AverageRating = Math.Round(avgRating, 2);

                    // NPS = (Promoters - Detractors) / Total * 100
                    response.NpsMetrics.NpsScore = Math.Round((double)(promoters - detractors) / totalResponses * 100, 2);

                    // Response rate (based on delivered orders as approximate base)
                    var deliveredCount = await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= firstDayOfMonth && x.OrderStatus == OrderStatusDM.Delivered)
                        .CountAsync();
                    response.NpsMetrics.ResponseRate = deliveredCount > 0
                        ? Math.Round((double)totalResponses / deliveredCount * 100, 2)
                        : 0;
                }
            }
            catch { }

            #endregion

            #region GROWTH METRICS (DAU, MAU, User Growth)

            try
            {
                // DAU - Daily Active Users
                var todayStart = IstDateHelper.IstDayStartUtc(istToday);
                var todayEnd = IstDateHelper.IstDayEndUtc(istToday);
                var yesterdayStart = IstDateHelper.IstDayStartUtc(istToday.AddDays(-1));
                var yesterdayEnd = IstDateHelper.IstDayEndUtc(istToday.AddDays(-1));
                var growthWeekStart = IstDateHelper.IstDayStartUtc(istToday.AddDays(-7));
                var growthMonthStart = IstDateHelper.IstDayStartUtc(istToday.AddDays(-30));

                // Today's active users (users who placed orders or logged in)
                var todayActiveUsers = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= todayStart && x.CreatedAt < todayEnd)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                // Yesterday's active users
                var yesterdayActiveUsers = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= yesterdayStart && x.CreatedAt < yesterdayEnd)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                // Week average DAU
                var dailyActiveUserCounts = new List<int>();
                for (int i = 0; i < 7; i++)
                {
                    var dauDay = istToday.AddDays(-i);
                    var dauDayStart = IstDateHelper.IstDayStartUtc(dauDay);
                    var dauDayEnd = IstDateHelper.IstDayEndUtc(dauDay);
                    var dauCount = await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= dauDayStart && x.CreatedAt < dauDayEnd)
                        .Select(x => x.UserId)
                        .Distinct()
                        .CountAsync();
                    dailyActiveUserCounts.Add(dauCount);
                }
                var weekAverageDau = (int)dailyActiveUserCounts.Average();

                // Month average DAU
                var monthlyActiveUserCounts = new List<int>();
                for (int i = 0; i < 30; i++)
                {
                    var mauDay = istToday.AddDays(-i);
                    var mauDayStart = IstDateHelper.IstDayStartUtc(mauDay);
                    var mauDayEnd = IstDateHelper.IstDayEndUtc(mauDay);
                    var mauCount = await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= mauDayStart && x.CreatedAt < mauDayEnd)
                        .Select(x => x.UserId)
                        .Distinct()
                        .CountAsync();
                    monthlyActiveUserCounts.Add(mauCount);
                }
                var monthAverageDau = (int)monthlyActiveUserCounts.Average();

                // DAU change percentage
                var dauChangePercent = yesterdayActiveUsers > 0
                    ? Math.Round((double)(todayActiveUsers - yesterdayActiveUsers) / yesterdayActiveUsers * 100, 2)
                    : 0;

                // DAU trend (last 14 days)
                var dauTrend = new List<DauDataPointSM>();
                for (int i = 13; i >= 0; i--)
                {
                    var trendDay = istToday.AddDays(-i);
                    var trendDayStart = IstDateHelper.IstDayStartUtc(trendDay);
                    var trendDayEnd = IstDateHelper.IstDayEndUtc(trendDay);

                    var orders = await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= trendDayStart && x.CreatedAt < trendDayEnd)
                        .ToListAsync();

                    dauTrend.Add(new DauDataPointSM
                    {
                        Date = trendDay.ToString("yyyy-MM-dd"),
                        Count = orders.Select(x => x.UserId).Distinct().Count(),
                        UniqueOrders = orders.Count,
                        AppOpens = 0, // Would require app analytics integration
                        UniqueLogins = 0 // Would require login tracking
                    });
                }

                response.GrowthMetrics.DailyActiveUsers = new DailyActiveUsersSM
                {
                    Today = todayActiveUsers,
                    Yesterday = yesterdayActiveUsers,
                    WeekAverage = weekAverageDau,
                    MonthAverage = monthAverageDau,
                    ChangePercent = dauChangePercent,
                    Trend = dauTrend
                };

                // MAU - Monthly Active Users
                var thisMonthStart = IstDateHelper.IstDayStartUtc(new DateTime(istToday.Year, istToday.Month, 1));
                var prevMonthStart = IstDateHelper.IstDayStartUtc(thisMonthStart.AddMonths(-1));
                var prevMonthEnd = IstDateHelper.IstDayEndUtc(thisMonthStart.AddDays(-1));
                var quarterStart = IstDateHelper.IstDayStartUtc(thisMonthStart.AddMonths(-3));

                var currentMonthUsers = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= thisMonthStart)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                var previousMonthUsers = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= prevMonthStart && x.CreatedAt <= prevMonthEnd)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                var quarterUsers = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= quarterStart)
                    .Select(x => x.UserId)
                    .Distinct()
                    .CountAsync();

                var mauChangePercent = previousMonthUsers > 0
                    ? Math.Round((double)(currentMonthUsers - previousMonthUsers) / previousMonthUsers * 100, 2)
                    : 0;

                // MAU trend (last 6 months)
                var mauTrend = new List<MauDataPointSM>();
                for (int i = 5; i >= 0; i--)
                {
                    var monthStart = IstDateHelper.IstDayStartUtc(new DateTime(istToday.Year, istToday.Month, 1).AddMonths(-i));
                    var monthEnd = IstDateHelper.IstDayEndUtc(monthStart.AddMonths(1).AddDays(-1));

                    var monthOrders = await _apiDbContext.Order
                        .Where(x => x.CreatedAt >= monthStart && x.CreatedAt <= monthEnd)
                        .ToListAsync();

                    var activeUsers = monthOrders.Select(x => x.UserId).Distinct().ToList();

                    mauTrend.Add(new MauDataPointSM
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        Count = activeUsers.Count,
                        NewUsers = 0, // Would require user registration tracking
                        ReturningUsers = activeUsers.Count
                    });
                }

                response.GrowthMetrics.MonthlyActiveUsers = new MonthlyActiveUsersSM
                {
                    CurrentMonth = currentMonthUsers,
                    PreviousMonth = previousMonthUsers,
                    QuarterAverage = quarterUsers / 3,
                    ChangePercent = mauChangePercent,
                    Trend = mauTrend
                };

                // User Growth
                var totalUsers = await _apiDbContext.User.CountAsync();
                var newUsersToday = await _apiDbContext.User
                    .Where(x => x.CreatedAt >= todayStart && x.CreatedAt < todayEnd)
                    .CountAsync();
                var newUsersThisWeek = await _apiDbContext.User
                    .Where(x => x.CreatedAt >= growthWeekStart)
                    .CountAsync();
                var newUsersThisMonth = await _apiDbContext.User
                    .Where(x => x.CreatedAt >= thisMonthStart)
                    .CountAsync();

                // Daily signups (last 14 days)
                var dailySignups = new List<GrowthDataPointSM>();
                for (int i = 13; i >= 0; i--)
                {
                    var signupDay = istToday.AddDays(-i);
                    var signupDayStart = IstDateHelper.IstDayStartUtc(signupDay);
                    var signupDayEnd = IstDateHelper.IstDayEndUtc(signupDay);

                    var newUsersCount = await _apiDbContext.User
                        .Where(x => x.CreatedAt >= signupDayStart && x.CreatedAt < signupDayEnd)
                        .CountAsync();

                    dailySignups.Add(new GrowthDataPointSM
                    {
                        Date = signupDay.ToString("yyyy-MM-dd"),
                        NewUsers = newUsersCount,
                        ChurnedUsers = 0, // Would require churn tracking
                        NetGrowth = newUsersCount
                    });
                }

                response.GrowthMetrics.UserGrowth = new UserGrowthSM
                {
                    TotalUsers = totalUsers,
                    NewUsersToday = newUsersToday,
                    NewUsersThisWeek = newUsersThisWeek,
                    NewUsersThisMonth = newUsersThisMonth,
                    GrowthRate = totalUsers > 0 ? Math.Round((double)newUsersThisMonth / totalUsers * 100, 2) : 0,
                    DailySignups = dailySignups
                };
            }
            catch { }

            #endregion

            #region HEATMAP ANALYTICS

            try
            {
                // Hourly activity heatmap (based on orders)
                var last30Days = IstDateHelper.IstDayStartUtc(istToday.AddDays(-30));
                var hourlyActivity = new List<HourlyActivitySM>();

                var ordersByHour = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last30Days)
                    .GroupBy(x => x.CreatedAt.Value.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        OrderCount = g.Count(),
                        UserCount = g.Select(x => x.UserId).Distinct().Count()
                    })
                    .ToListAsync();

                var maxOrderCount = ordersByHour.Any() ? ordersByHour.Max(x => x.OrderCount) : 1;

                for (int hour = 0; hour < 24; hour++)
                {
                    var hourData = ordersByHour.FirstOrDefault(x => x.Hour == hour);
                    var orderCount = hourData?.OrderCount ?? 0;
                    var userCount = hourData?.UserCount ?? 0;

                    hourlyActivity.Add(new HourlyActivitySM
                    {
                        Hour = hour,
                        HourLabel = hour == 0 ? "12 AM" : hour < 12 ? $"{hour} AM" : hour == 12 ? "12 PM" : $"{hour - 12} PM",
                        UserCount = userCount,
                        OrderCount = orderCount,
                        SessionCount = orderCount, // Approximated by orders
                        Intensity = maxOrderCount > 0 ? (double)orderCount / maxOrderCount : 0
                    });
                }

                // Day of week activity
                var dayOfWeekActivity = new List<DayOfWeekActivitySM>();
                var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

                var ordersByDay = await _apiDbContext.Order
                    .Where(x => x.CreatedAt >= last30Days)
                    .ToListAsync();

                var groupedByDay = ordersByDay
                    .GroupBy(x => (int)(x.CreatedAt ?? DateTime.UtcNow).DayOfWeek)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            OrderCount = g.Count(),
                            UserCount = g.Select(x => x.UserId).Distinct().Count()
                        });

                var maxDayCount = groupedByDay.Any() ? groupedByDay.Max(x => x.Value.OrderCount) : 1;

                for (int day = 0; day < 7; day++)
                {
                    var dayData = groupedByDay.ContainsKey(day) ? groupedByDay[day] : null;
                    var orderCount = dayData?.OrderCount ?? 0;
                    var userCount = dayData?.UserCount ?? 0;

                    dayOfWeekActivity.Add(new DayOfWeekActivitySM
                    {
                        DayOfWeek = day,
                        DayName = dayNames[day],
                        UserCount = userCount,
                        OrderCount = orderCount,
                        SessionCount = orderCount,
                        Intensity = maxDayCount > 0 ? (double)orderCount / maxDayCount : 0
                    });
                }

                // Geographic activity (by city)
                var geographicActivity = await _apiDbContext.UserAddress
                    .Where(x => !string.IsNullOrEmpty(x.City))
                    .GroupBy(x => new { x.City, x.State })
                    .Select(g => new GeographicActivitySM
                    {
                        City = g.Key.City,
                        State = g.Key.State ?? "Unknown",
                        UserCount = g.Select(x => x.UserId).Distinct().Count(),
                        OrderCount = 0, // Would require address-order linkage
                        Revenue = 0,
                        Latitude = 0, // Would require geocoding
                        Longitude = 0,
                        Intensity = 0
                    })
                    .OrderByDescending(x => x.UserCount)
                    .Take(20)
                    .ToListAsync();

                // Page views (placeholder - would require analytics tracking)
                var pageViews = new List<PageViewHeatmapSM>
                {
                    new() { PagePath = "/", PageName = "Home", ViewCount = 0, UniqueVisitors = 0, AvgTimeOnPage = 0, BounceRate = 0, Intensity = 0 },
                    new() { PagePath = "/products", PageName = "Products", ViewCount = 0, UniqueVisitors = 0, AvgTimeOnPage = 0, BounceRate = 0, Intensity = 0 },
                    new() { PagePath = "/cart", PageName = "Cart", ViewCount = 0, UniqueVisitors = 0, AvgTimeOnPage = 0, BounceRate = 0, Intensity = 0 },
                    new() { PagePath = "/checkout", PageName = "Checkout", ViewCount = 0, UniqueVisitors = 0, AvgTimeOnPage = 0, BounceRate = 0, Intensity = 0 },
                    new() { PagePath = "/orders", PageName = "Orders", ViewCount = 0, UniqueVisitors = 0, AvgTimeOnPage = 0, BounceRate = 0, Intensity = 0 }
                };

                // Find peak values
                var peakHour = hourlyActivity.OrderByDescending(x => x.OrderCount).FirstOrDefault();
                var peakDay = dayOfWeekActivity.OrderByDescending(x => x.OrderCount).FirstOrDefault();
                var topCity = geographicActivity.FirstOrDefault();

                response.Heatmap = new HeatmapSM
                {
                    HourlyActivity = hourlyActivity,
                    DayOfWeekActivity = dayOfWeekActivity,
                    GeographicActivity = geographicActivity,
                    PageViews = pageViews,
                    Summary = new HeatmapSummarySM
                    {
                        PeakHour = peakHour?.Hour ?? 0,
                        PeakHourLabel = peakHour?.HourLabel ?? "N/A",
                        PeakDayOfWeek = peakDay?.DayOfWeek ?? 0,
                        PeakDayLabel = peakDay?.DayName ?? "N/A",
                        TopCity = topCity?.City ?? "N/A",
                        TopPage = "Home", // Default
                        AverageSessionDuration = 0, // Would require session tracking
                        TotalSessions = ordersByHour.Sum(x => x.OrderCount)
                    }
                };
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
                    CreatedAt = o.CreatedAt,
                    PlatformType = o.PlatformType.ToString()
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
