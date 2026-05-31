using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Base;
using Siffrum.Ecom.BAL.Base.Email;
using Siffrum.Ecom.BAL.Base.ImageProcess;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1.Dashboard.SellerDashboard;

namespace Siffrum.Ecom.BAL.LoginUsers
{
    public class SellerDashboardProcess : SiffrumBalBase
    {

        public SellerDashboardProcess(IMapper mapper,ApiDbContext apiDbContext)
            : base(mapper, apiDbContext)
        {
        }

        public async Task<SellerDashboardSM> GetSellerDashboard(long sellerId, DateTime? date = null)
        {
            return new SellerDashboardSM
            {
                Kpis = await GetKpis(sellerId, date),
                SalesGraph = await GetSalesGraph(sellerId),
                Orders = await GetOrderSnapshot(sellerId, date),
                Products = await GetProductSnapshot(sellerId),
                Financial = await GetFinancialSnapshot(sellerId),
                Customers = await GetCustomerInsights(sellerId)
            };
        }

        private async Task<SellerKpiSM> GetKpis(long sellerId, DateTime? date = null)
        {
            var dayStart = IstDateHelper.IstDayStartUtc(date?.Date);
            var dayEnd = IstDateHelper.IstDayEndUtc(date?.Date);

            var todayRevenue = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus == OrderStatusDM.Delivered &&
                    x.CreatedAt >= dayStart && x.CreatedAt < dayEnd)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var ordersToday = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus != OrderStatusDM.AwaitingPayment &&
                    x.CreatedAt >= dayStart && x.CreatedAt < dayEnd)
                .CountAsync();

            var pendingOrders = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus == OrderStatusDM.Created)
                .CountAsync();

            var lowStock = await _apiDbContext.ProductVariant
                .Where(x =>
                    x.Product.SellerId == sellerId &&
                    x.Stock.HasValue &&
                    x.Stock < 5)
                .CountAsync();

            var rating = await _apiDbContext.ProductRating
                .Where(x => x.ProductVariant.Product.SellerId == sellerId)
                .AverageAsync(x => (double?)x.Rate) ?? 0;

            var seller = await _apiDbContext.Seller.FindAsync(sellerId);
            var commRate = seller?.Commission ?? 0;

            var totalDelivered = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus == OrderStatusDM.Delivered)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var pendingPayout = totalDelivered - (totalDelivered * commRate / 100);

            return new SellerKpiSM
            {
                TodayRevenue = todayRevenue,
                OrdersToday = ordersToday,
                PendingOrders = pendingOrders,
                LowStockCount = lowStock,
                PendingPayoutAmount = pendingPayout,
                StoreRating = rating
            };
        }

        private async Task<SellerSalesGraphSM> GetSalesGraph(long sellerId)
        {
            var istOffset = TimeSpan.FromHours(5.5);
            var todayIst = IstDateHelper.Now.Date;
            var startDate = IstDateHelper.IstDayStartUtc(todayIst.AddDays(-6));

            var rawOrders = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus == OrderStatusDM.Delivered &&
                    x.CreatedAt >= startDate)
                .Select(x => new { x.Id, x.Amount, x.CreatedAt })
                .ToListAsync();

            var grouped = rawOrders
                .GroupBy(x => (x.CreatedAt ?? DateTime.UtcNow).Add(istOffset).Date)
                .ToDictionary(
                    g => g.Key,
                    g => new SalesGraphPointSM
                    {
                        Date = g.Key,
                        Revenue = g.Sum(i => i.Amount),
                        OrderCount = g.Count()
                    });

            var filled = new List<SalesGraphPointSM>();
            for (int i = -6; i <= 0; i++)
            {
                var day = todayIst.AddDays(i);
                if (grouped.TryGetValue(day, out var point))
                    filled.Add(point);
                else
                    filled.Add(new SalesGraphPointSM { Date = day, Revenue = 0, OrderCount = 0 });
            }

            return new SellerSalesGraphSM
            {
                Data = filled,
                GrowthPercentage = 0
            };
        }

        private async Task<SellerOrderSnapshotSM> GetOrderSnapshot(long sellerId, DateTime? date = null)
        {
            var dayStart = IstDateHelper.IstDayStartUtc(date?.Date);
            var dayEnd = IstDateHelper.IstDayEndUtc(date?.Date);

            var query = _apiDbContext.Order
                .Where(x => x.SellerId == sellerId
                    && x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);

            var globalQuery = _apiDbContext.Order
                .Where(x => x.SellerId == sellerId);

            return new SellerOrderSnapshotSM
            {
                Pending = await query.Where(x => x.OrderStatus == OrderStatusDM.Created)
                                     .CountAsync(),

                Accepted = await query.Where(x => x.OrderStatus == OrderStatusDM.SellerAccepted)
                                      .CountAsync(),

                Processing = await query.Where(x =>
                                        x.OrderStatus == OrderStatusDM.Processing ||
                                        x.OrderStatus == OrderStatusDM.Assigned ||
                                        x.OrderStatus == OrderStatusDM.PickedUp ||
                                        x.OrderStatus == OrderStatusDM.OutForDelivery)
                                        .CountAsync(),

                Shipped = await query.Where(x => x.OrderStatus == OrderStatusDM.Shipped)
                                     .CountAsync(),

                Delivered = await query.Where(x => x.OrderStatus == OrderStatusDM.Delivered)
                                       .CountAsync(),

                Cancelled = await query.Where(x =>
                                        x.OrderStatus == OrderStatusDM.Cancelled ||
                                        x.OrderStatus == OrderStatusDM.CancelledBySeller)
                                        .CountAsync(),

                Returned = await query.Where(x => x.OrderStatus == OrderStatusDM.Returned)
                                      .CountAsync(),

                InQueueOrders = await globalQuery.Where(x =>
                                        x.OrderStatus == OrderStatusDM.SellerAccepted ||
                                        x.OrderStatus == OrderStatusDM.Processing ||
                                        x.OrderStatus == OrderStatusDM.Shipped ||
                                        x.OrderStatus == OrderStatusDM.Assigned ||
                                        x.OrderStatus == OrderStatusDM.PickedUp ||
                                        x.OrderStatus == OrderStatusDM.OutForDelivery)
                                        .CountAsync()
            };
        }

        private async Task<SellerProductSnapshotSM> GetProductSnapshot(long sellerId)
        {
            var totalActive = await _apiDbContext.ProductVariant
                .Where(x =>
                    x.Product.SellerId == sellerId &&
                    x.Status == ProductStatusDM.Active)
                .CountAsync();

            var outOfStock = await _apiDbContext.ProductVariant
                .Where(x =>
                    x.Product.SellerId == sellerId &&
                    x.Stock <= 0)
                .CountAsync();

            var rejected = await _apiDbContext.ProductVariant
                .Where(x =>
                    x.Product.SellerId == sellerId &&
                    x.Status == ProductStatusDM.Rejected)
                .CountAsync();

            var topProductsRaw = await _apiDbContext.OrderItem
                .Where(x =>
                    x.ProductVariant.Product.SellerId == sellerId &&
                    x.Order.PaymentStatus == PaymentStatusDM.Paid &&
                    x.Order.OrderStatus == OrderStatusDM.Delivered)
                .GroupBy(x => new
                {
                    x.ProductVariantId,
                    ProductName = x.ProductVariant.Product.Name,
                    VariantName = x.ProductVariant.Name
                })
                .Select(g => new
                {
                    g.Key.ProductVariantId,
                    g.Key.ProductName,
                    g.Key.VariantName,
                    QuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            var topProducts = topProductsRaw.Select(x => new TopSellingProductSM
            {
                ProductVariantId = x.ProductVariantId,
                Name = string.IsNullOrEmpty(x.VariantName) ? x.ProductName : $"{x.ProductName} ({x.VariantName})",
                QuantitySold = x.QuantitySold
            }).ToList();

            return new SellerProductSnapshotSM
            {
                TotalActiveProducts = totalActive,
                OutOfStock = outOfStock,
                RejectedProducts = rejected,
                TopSellingProducts = topProducts
            };
        }

        private async Task<SellerFinancialSnapshotSM> GetFinancialSnapshot(long sellerId)
        {
            var seller = await _apiDbContext.Seller.FindAsync(sellerId);
            var commissionRate = seller?.Commission ?? 0;

            var deliveredTotal = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus == OrderStatusDM.Delivered)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var commissionPaid = deliveredTotal * commissionRate / 100;
            var availableBalance = deliveredTotal - commissionPaid;

            var lockedBalance = await _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus != OrderStatusDM.Delivered &&
                    x.OrderStatus != OrderStatusDM.Returned &&
                    x.OrderStatus != OrderStatusDM.Cancelled &&
                    x.OrderStatus != OrderStatusDM.CancelledBySeller &&
                    x.OrderStatus != OrderStatusDM.Failed &&
                    x.OrderStatus != OrderStatusDM.AwaitingPayment)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            return new SellerFinancialSnapshotSM
            {
                AvailableBalance = availableBalance,
                LockedBalance = lockedBalance,
                CommissionPaid = commissionPaid,
                UpcomingPayoutDate = null
            };
        }

        private async Task<SellerCustomerInsightsSM> GetCustomerInsights(long sellerId)
        {
            var sellerOrders = _apiDbContext.Order
                .Where(x =>
                    x.SellerId == sellerId &&
                    x.OrderStatus == OrderStatusDM.Delivered);

            var totalCustomers = await sellerOrders
                .Select(x => x.UserId)
                .Distinct()
                .CountAsync();

            var repeatCustomers = await sellerOrders
                .GroupBy(x => x.UserId)
                .Where(g => g.Count() > 1)
                .CountAsync();

            var orderCount = await sellerOrders.CountAsync();

            var totalRevenue = orderCount == 0 ? 0
                : await sellerOrders.SumAsync(x => (decimal?)x.Amount) ?? 0;

            var avgOrderValue = orderCount == 0 ? 0 : totalRevenue / orderCount;

            // Best category still needs OrderItem join for product-level data
            var bestCategory = await _apiDbContext.OrderItem
                .Where(x =>
                    x.Order.SellerId == sellerId &&
                    x.Order.OrderStatus == OrderStatusDM.Delivered)
                .GroupBy(x => x.ProductVariant.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(x => x.TotalPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Select(x => x.Category)
                .FirstOrDefaultAsync();

            return new SellerCustomerInsightsSM
            {
                RepeatCustomerPercentage = totalCustomers == 0
                    ? 0
                    : (double)repeatCustomers / totalCustomers * 100,

                AverageOrderValue = avgOrderValue,
                BestPerformingCategory = bestCategory
            };
        }

        public async Task<List<SellerOrderDrillDownItemSM>> GetOrdersByStatusAsync(
            long sellerId, DateTime date, string statusFilter)
        {
            var isLiveStatus = statusFilter?.ToLower() == "inqueue";

            var baseQuery = _apiDbContext.Order
                .Where(x => x.SellerId == sellerId
                    && x.OrderStatus != OrderStatusDM.AwaitingPayment);

            if (!isLiveStatus)
            {
                var dayStart = IstDateHelper.IstDayStartUtc(date.Date);
                var dayEnd = IstDateHelper.IstDayEndUtc(date.Date);
                baseQuery = baseQuery.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
            }

            baseQuery = ApplySellerOrderStatusFilter(baseQuery, statusFilter);

            var rows = await baseQuery
                .Select(x => new SellerOrderDrillDownItemSM
                {
                    OrderId = x.Id,
                    OrderNumber = x.OrderNumber,
                    Amount = x.Amount,
                    OrderStatus = x.OrderStatus.ToString(),
                    PaymentStatus = x.PaymentStatus.ToString(),
                    PaymentMode = x.PaymentMode.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return rows;
        }

        public async Task<List<LowStockPlatformSummarySM>> GetLowStockSummaryAsync(long sellerId)
        {
            var counts = await _apiDbContext.ProductVariant
                .AsNoTracking()
                .Where(v => v.Product.SellerId == sellerId
                    && v.Stock.HasValue && v.Stock < 5)
                .GroupBy(v => v.PlatformType)
                .Select(g => new { Platform = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.Select(c => new LowStockPlatformSummarySM
            {
                Platform = c.Platform.ToString(),
                Count = c.Count
            }).OrderByDescending(x => x.Count).ToList();
        }

        public async Task<List<LowStockVariantSM>> GetLowStockItemsAsync(
            long sellerId, string platform)
        {
            var query = _apiDbContext.ProductVariant
                .AsNoTracking()
                .Include(v => v.Product)
                .Where(v => v.Product.SellerId == sellerId
                    && v.Stock.HasValue && v.Stock < 5);

            if (Enum.TryParse<PlatformTypeDM>(platform, true, out var pt) && pt != PlatformTypeDM.None)
                query = query.Where(v => v.PlatformType == pt);

            return await query
                .OrderBy(v => v.Stock)
                .Select(v => new LowStockVariantSM
                {
                    VariantId = v.Id,
                    ProductName = v.Product.Name ?? "",
                    VariantName = v.Name ?? "",
                    Image = v.Image,
                    Stock = (double)(v.Stock ?? 0),
                    Price = v.Price,
                    CategoryName = v.Product.Category != null ? v.Product.Category.Name : null
                })
                .ToListAsync();
        }

        private static IQueryable<DomainModels.v1.OrderDM> ApplySellerOrderStatusFilter(
            IQueryable<DomainModels.v1.OrderDM> query, string statusFilter)
        {
            switch (statusFilter?.ToLower())
            {
                case "all":
                    return query;
                case "pending":
                    return query.Where(x => x.OrderStatus == OrderStatusDM.Created);
                case "delivered":
                    return query.Where(x => x.OrderStatus == OrderStatusDM.Delivered);
                case "cancelled":
                    return query.Where(x => x.OrderStatus == OrderStatusDM.Cancelled
                        || x.OrderStatus == OrderStatusDM.CancelledBySeller);
                case "returned":
                    return query.Where(x => x.OrderStatus == OrderStatusDM.Returned);
                case "failed":
                    return query.Where(x => x.OrderStatus == OrderStatusDM.Failed);
                case "inqueue":
                    return query.Where(x => x.OrderStatus == OrderStatusDM.SellerAccepted
                        || x.OrderStatus == OrderStatusDM.Processing
                        || x.OrderStatus == OrderStatusDM.Assigned
                        || x.OrderStatus == OrderStatusDM.PickedUp
                        || x.OrderStatus == OrderStatusDM.OutForDelivery
                        || x.OrderStatus == OrderStatusDM.Shipped);
                default:
                    return query;
            }
        }
    }
}
