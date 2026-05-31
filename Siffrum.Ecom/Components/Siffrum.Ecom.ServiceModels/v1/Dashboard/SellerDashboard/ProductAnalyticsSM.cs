namespace Siffrum.Ecom.ServiceModels.v1.Dashboard.SellerDashboard
{
    /// <summary>
    /// Product analytics dashboard data for seller
    /// </summary>
    public class ProductAnalyticsSM
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PeriodLabel { get; set; } = string.Empty;
        
        // Summary KPIs
        public ProductAnalyticsKpiSM Kpis { get; set; } = new();
        
        // Product-wise breakdown
        public List<ProductPerformanceSM> Products { get; set; } = new();
        
        // Daily/period trend for chart
        public List<ProductSalesTrendPointSM> Trend { get; set; } = new();
        
        // Category performance
        public List<CategoryPerformanceSM> Categories { get; set; } = new();
    }

    public class ProductAnalyticsKpiSM
    {
        public decimal TotalRevenue { get; set; }
        public int TotalQuantitySold { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveProductsCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public string TopProductName { get; set; } = string.Empty;
        public int TopProductQuantity { get; set; }
    }

    public class ProductPerformanceSM
    {
        public long ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string? CategoryName { get; set; }
        public string? ImageUrl { get; set; }
        
        // Sales metrics
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
        public decimal AverageSellingPrice { get; set; }
        
        // Performance percentage (for visual indicators)
        public double RevenuePercentageOfTotal { get; set; }
        public double QuantityPercentageOfTotal { get; set; }
    }

    public class ProductSalesTrendPointSM
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Quantity { get; set; }
        public int OrdersCount { get; set; }
    }

    public class CategoryPerformanceSM
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public double RevenuePercentage { get; set; }
    }
}
