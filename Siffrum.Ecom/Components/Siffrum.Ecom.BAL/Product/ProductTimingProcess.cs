using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.Enums;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class ProductTimingProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ProductVariantProcess _productVariantProcess;

        public ProductTimingProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail,
            ProductVariantProcess productVariantProcess)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _productVariantProcess = productVariantProcess;
        }

        #region Seller CRUD

        public async Task<ProductTimingSM> Create(ProductTimingSM sm)
        {
            // Validate time slot doesn't cross midnight
            ValidateTimeSlot(sm.StartHour, sm.StartMinute, sm.EndHour, sm.EndMinute);

            var dm = new ProductTimingDM
            {
                SellerId = _loginUserDetail.DbRecordId,
                ProductId = sm.ProductId,
                CategoryId = sm.CategoryId,
                StartHour = sm.StartHour,
                StartMinute = sm.StartMinute,
                EndHour = sm.EndHour,
                EndMinute = sm.EndMinute,
                IsActive = true,
                CreatedBy = _loginUserDetail.LoginId,
                CreatedAt = DateTime.UtcNow
            };

            await _apiDbContext.ProductTimings.AddAsync(dm);
            await _apiDbContext.SaveChangesAsync();

            sm.Id = dm.Id;
            sm.SellerId = dm.SellerId;
            sm.IsActive = dm.IsActive;
            return sm;
        }

        public async Task<List<ProductTimingSM>> GetBySeller(int skip, int top)
        {
            var sellerId = _loginUserDetail.DbRecordId;

            return await _apiDbContext.ProductTimings
                .AsNoTracking()
                .Where(x => x.SellerId == sellerId)
                .OrderByDescending(x => x.Id)
                .Skip(skip).Take(top)
                .Select(x => new ProductTimingSM
                {
                    Id = x.Id,
                    SellerId = x.SellerId,
                    ProductId = x.ProductId,
                    CategoryId = x.CategoryId,
                    StartHour = x.StartHour,
                    StartMinute = x.StartMinute,
                    EndHour = x.EndHour,
                    EndMinute = x.EndMinute,
                    IsActive = x.IsActive,
                    ProductName = x.Product.Name,
                    CategoryName = x.Category.Name
                })
                .ToListAsync();
        }

        public async Task<IntResponseRoot> GetBySellerCount()
        {
            var sellerId = _loginUserDetail.DbRecordId;
            var count = await _apiDbContext.ProductTimings
                .AsNoTracking()
                .Where(x => x.SellerId == sellerId)
                .CountAsync();
            return new IntResponseRoot(count, "Total product timings");
        }

        public async Task<ProductTimingSM> Update(long id, ProductTimingSM sm)
        {
            var dm = await _apiDbContext.ProductTimings
                .FirstOrDefaultAsync(x => x.Id == id && x.SellerId == _loginUserDetail.DbRecordId);
            if (dm == null) return null;

            // Validate time slot doesn't cross midnight
            ValidateTimeSlot(sm.StartHour, sm.StartMinute, sm.EndHour, sm.EndMinute);

            dm.ProductId = sm.ProductId;
            dm.CategoryId = sm.CategoryId;
            dm.StartHour = sm.StartHour;
            dm.StartMinute = sm.StartMinute;
            dm.EndHour = sm.EndHour;
            dm.EndMinute = sm.EndMinute;
            dm.IsActive = sm.IsActive;
            dm.UpdatedBy = _loginUserDetail.LoginId;
            dm.UpdatedAt = DateTime.UtcNow;

            await _apiDbContext.SaveChangesAsync();

            sm.Id = dm.Id;
            sm.SellerId = dm.SellerId;
            return sm;
        }

        /// <summary>
        /// Validates that time slot doesn't cross midnight.
        /// Each day starts at 00:00 (12:00 AM) and ends at 23:59.
        /// </summary>
        private void ValidateTimeSlot(int startHour, int startMinute, int endHour, int endMinute)
        {
            // Convert to total minutes for comparison
            var startTotalMinutes = startHour * 60 + startMinute;
            var endTotalMinutes = endHour * 60 + endMinute;

            // Check if time slot crosses midnight (end time is earlier than start time)
            if (endTotalMinutes <= startTotalMinutes)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_NoLog,
                    "Invalid time slot",
                    "Time slots cannot cross midnight. Each day starts at 12:00 AM (midnight). " +
                    "If you need coverage across midnight, please create two separate time slots: " +
                    "one from your start time to 11:59 PM, and another from 12:00 AM to your end time.");
            }

            // Validate hours and minutes are in valid ranges
            if (startHour < 0 || startHour > 23 || endHour < 0 || endHour > 23)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Hours must be between 0 and 23");
            
            if (startMinute < 0 || startMinute > 59 || endMinute < 0 || endMinute > 59)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Minutes must be between 0 and 59");
        }

        public async Task<BoolResponseRoot> Delete(long id)
        {
            var dm = await _apiDbContext.ProductTimings
                .FirstOrDefaultAsync(x => x.Id == id && x.SellerId == _loginUserDetail.DbRecordId);
            if (dm == null) return new BoolResponseRoot(false, "Not found");

            _apiDbContext.ProductTimings.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new BoolResponseRoot(true, "Deleted");
        }

        #endregion Seller CRUD

        #region User Timing Query

        public async Task<UserHotBoxCategoryProductsSM> GetProductsByTiming(
            CategoryTimingSM timing, int skip, int top)
        {
            // Convert UTC to IST (Asia/Kolkata) for consistent timing checks
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
            var currentHour = istTime.Hour;

            // If timing is not specified, determine from current IST hour
            if (timing == CategoryTimingSM.None || timing == CategoryTimingSM.AllDay)
            {
                timing = GetTimingFromHour(currentHour);
            }

            var (startHour, endHour) = MapTimingToHours(timing);

            // Find all active product timings whose range overlaps the requested slot
            var query = _apiDbContext.ProductTimings
                .AsNoTracking()
                .Where(x => x.IsActive && x.Product.ProductVariants.Any(
                    v => v.PlatformType == PlatformTypeDM.HotBox && v.Status == ProductStatusDM.Active));

            // Overlap logic: timing slot [startHour..endHour) overlaps entry [StartHour..EndHour)
            // Handles cross-midnight too (e.g. 22:00 → 05:00)
            query = query.Where(x =>
                // Normal range (no midnight crossing on either side)
                (x.StartHour < x.EndHour && startHour < endHour &&
                    x.StartHour < endHour && startHour < x.EndHour) ||
                // Entry crosses midnight
                (x.StartHour >= x.EndHour &&
                    (startHour < x.EndHour || startHour >= x.StartHour || endHour > x.StartHour || endHour <= x.EndHour)) ||
                // Slot crosses midnight  
                (startHour >= endHour &&
                    (x.StartHour < endHour || x.StartHour >= startHour || x.EndHour > startHour || x.EndHour <= endHour))
            );

            var timings = await query
                .OrderByDescending(x => x.Id)
                .Skip(skip).Take(top)
                .Select(x => new
                {
                    x.ProductId,
                    x.CategoryId,
                    CategoryName = x.Category.Name
                })
                .ToListAsync();

            if (timings.Count == 0)
            {
                return new UserHotBoxCategoryProductsSM
                {
                    Id = 0,
                    CategoryName = "No items available",
                    Products = new List<UserHotBoxProductSM>(),
                    ComboProducts = new List<ComboProductSM>()
                };
            }

            // Get one variant per product (lowest priced)
            var productIds = timings.Select(x => x.ProductId).Distinct().ToList();
            var nearbySellerIds = await _productVariantProcess.GetNearbySellerIds();
            var varQuery = _apiDbContext.ProductVariant
                .AsNoTracking()
                .Where(v => productIds.Contains(v.ProductId) &&
                            v.PlatformType == PlatformTypeDM.HotBox &&
                            v.Status == ProductStatusDM.Active);
            if (nearbySellerIds != null)
                varQuery = varQuery.Where(v => nearbySellerIds.Contains(v.Product.SellerId));
            var variantIds = await varQuery
                .GroupBy(v => v.ProductId)
                .Select(g => g.OrderBy(v => v.DiscountedPrice > 0 ? v.DiscountedPrice : v.Price).First().Id)
                .ToListAsync();

            var products = await _productVariantProcess.GetHotBoxProductsByBanner(variantIds);

            var firstCat = timings.First();
            return new UserHotBoxCategoryProductsSM
            {
                Id = firstCat.CategoryId,
                CategoryName = firstCat.CategoryName,
                Products = products,
                ComboProducts = new List<ComboProductSM>()
            };
        }

        public async Task<IntResponseRoot> GetProductsByTimingCount(CategoryTimingSM timing)
        {
            // Convert UTC to IST (Asia/Kolkata) for consistent timing checks
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
            var currentHour = istTime.Hour;

            // If timing is not specified, determine from current IST hour
            if (timing == CategoryTimingSM.None || timing == CategoryTimingSM.AllDay)
            {
                timing = GetTimingFromHour(currentHour);
            }

            var (startHour, endHour) = MapTimingToHours(timing);

            var query = _apiDbContext.ProductTimings
                .AsNoTracking()
                .Where(x => x.IsActive && x.Product.ProductVariants.Any(
                    v => v.PlatformType == PlatformTypeDM.HotBox && v.Status == ProductStatusDM.Active));

            query = query.Where(x =>
                (x.StartHour < x.EndHour && startHour < endHour &&
                    x.StartHour < endHour && startHour < x.EndHour) ||
                (x.StartHour >= x.EndHour &&
                    (startHour < x.EndHour || startHour >= x.StartHour || endHour > x.StartHour || endHour <= x.EndHour)) ||
                (startHour >= endHour &&
                    (x.StartHour < endHour || x.StartHour >= startHour || x.EndHour > startHour || x.EndHour <= endHour))
            );

            var count = await query.Select(x => x.ProductId).Distinct().CountAsync();
            return new IntResponseRoot(count, "Total products for timing");
        }

        private static (int startHour, int endHour) MapTimingToHours(CategoryTimingSM timing)
        {
            return timing switch
            {
                CategoryTimingSM.EarlyMorning => (5, 8),
                CategoryTimingSM.Morning => (8, 11),
                CategoryTimingSM.Brunch => (11, 13),
                CategoryTimingSM.Lunch => (13, 16),
                CategoryTimingSM.Evening => (16, 19),
                CategoryTimingSM.Dinner => (19, 22),
                CategoryTimingSM.LateNight => (22, 5),   // crosses midnight
                CategoryTimingSM.AllDay => (0, 24),
                _ => (0, 24)
            };
        }

        private static CategoryTimingSM GetTimingFromHour(int hour)
        {
            return hour switch
            {
                >= 5 and < 8 => CategoryTimingSM.EarlyMorning,
                >= 8 and < 11 => CategoryTimingSM.Morning,
                >= 11 and < 13 => CategoryTimingSM.Brunch,
                >= 13 and < 16 => CategoryTimingSM.Lunch,
                >= 16 and < 19 => CategoryTimingSM.Evening,
                >= 19 and < 22 => CategoryTimingSM.Dinner,
                _ => CategoryTimingSM.LateNight
            };
        }

        #endregion User Timing Query

        #region Diagnostics

        /// <summary>
        /// Diagnose why a product's timing isn't showing in the app
        /// </summary>
        public async Task<ProductTimingDiagnosticsSM> DiagnoseProductTiming(long productId, string timezone = "Asia/Kolkata")
        {
            var diagnostics = new ProductTimingDiagnosticsSM
            {
                ProductId = productId,
                CheckedAt = DateTime.UtcNow,
                Timezone = timezone
            };

            // Get current time in specified timezone
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            diagnostics.CurrentTime = now.ToString("HH:mm");
            diagnostics.CurrentHour = now.Hour;

            // Check if product exists and has active variants
            var product = await _apiDbContext.Product
                .AsNoTracking()
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                diagnostics.Issues.Add("Product not found in database");
                return diagnostics;
            }

            diagnostics.ProductName = product.Name;
            diagnostics.HasActiveVariants = product.ProductVariants?.Any(v => v.Status == ProductStatusDM.Active) ?? false;

            if (!diagnostics.HasActiveVariants)
            {
                diagnostics.Issues.Add("Product has no active variants");
            }

            // Check if product has HotBox variants
            var hasHotBoxVariants = product.ProductVariants?.Any(v => v.PlatformType == PlatformTypeDM.HotBox && v.Status == ProductStatusDM.Active) ?? false;
            if (!hasHotBoxVariants)
            {
                diagnostics.Issues.Add("Product has no HotBox platform variants");
            }

            // Get all timings for this product
            var timings = await _apiDbContext.ProductTimings
                .AsNoTracking()
                .Where(t => t.ProductId == productId && t.IsActive)
                .ToListAsync();

            diagnostics.TimingCount = timings.Count;

            if (timings.Count == 0)
            {
                diagnostics.Issues.Add("No active product timings found. Product will not show in HotBox without timings.");
                return diagnostics;
            }

            // Check each timing
            foreach (var timing in timings)
            {
                var timingCheck = new TimingCheckDetail
                {
                    TimingId = timing.Id,
                    StartTime = $"{timing.StartHour:D2}:{timing.StartMinute:D2}",
                    EndTime = $"{timing.EndHour:D2}:{timing.EndMinute:D2}",
                    CrossesMidnight = timing.StartHour > timing.EndHour || (timing.StartHour == timing.EndHour && timing.StartMinute >= timing.EndMinute)
                };

                // Check if current time falls within this slot
                var currentMinutes = now.Hour * 60 + now.Minute;
                var startMinutes = timing.StartHour * 60 + timing.StartMinute;
                var endMinutes = timing.EndHour * 60 + timing.EndMinute;

                if (timingCheck.CrossesMidnight)
                {
                    // Crosses midnight - currently would need complex logic
                    timingCheck.IsCurrentlyActive = currentMinutes >= startMinutes || currentMinutes < endMinutes;
                    timingCheck.Issues.Add("⚠️ Time slot crosses midnight - this is not allowed and may cause issues");
                }
                else
                {
                    // Normal slot
                    timingCheck.IsCurrentlyActive = currentMinutes >= startMinutes && currentMinutes < endMinutes;
                }

                if (timingCheck.IsCurrentlyActive)
                {
                    diagnostics.IsCurrentlyAvailable = true;
                }

                diagnostics.TimingChecks.Add(timingCheck);
            }

            // Determine category timing that should match
            var categoryTiming = GetTimingFromHour(now.Hour);
            diagnostics.ExpectedCategoryTiming = categoryTiming.ToString();
            var (expectedStart, expectedEnd) = MapTimingToHours(categoryTiming);
            diagnostics.ExpectedTimeRange = $"{expectedStart:D2}:00 - {expectedEnd:D2}:00";

            // Check if any timing overlaps with expected category timing
            var hasMatchingTiming = timings.Any(t =>
            {
                var timingStart = t.StartHour;
                var timingEnd = t.EndHour;
                // Simple overlap check for normal ranges
                if (timingStart < timingEnd && expectedStart < expectedEnd)
                {
                    return timingStart < expectedEnd && expectedStart < timingEnd;
                }
                return false;
            });

            if (!hasMatchingTiming)
            {
                diagnostics.Issues.Add($"None of the product timings overlap with current category timing ({categoryTiming}: {expectedStart}:00-{expectedEnd}:00). " +
                    "The product timing must cover at least part of the current time slot to appear in the app.");
            }

            if (!diagnostics.IsCurrentlyAvailable && timings.Count > 0)
            {
                diagnostics.Issues.Add($"Current time ({diagnostics.CurrentTime}) does not fall within any active product timing slot");
            }

            // Recommendations
            if (timings.Any(t => t.StartHour > t.EndHour || (t.StartHour == t.EndHour && t.StartMinute >= t.EndMinute)))
            {
                diagnostics.Recommendations.Add("Fix timings that cross midnight. Create two separate slots instead (e.g., 22:00-23:59 and 00:00-02:00).");
            }

            if (!diagnostics.IsCurrentlyAvailable)
            {
                var activeTiming = timings.FirstOrDefault(t =>
                {
                    var start = t.StartHour * 60 + t.StartMinute;
                    var end = t.EndHour * 60 + t.EndMinute;
                    return start < end; // Valid non-crossing slot
                });
                if (activeTiming != null)
                {
                    diagnostics.Recommendations.Add($"Current timing: {activeTiming.StartHour:D2}:{activeTiming.StartMinute:D2} - {activeTiming.EndHour:D2}:{activeTiming.EndMinute:D2}. " +
                        "Check if this covers the current category time slot.");
                }
            }

            return diagnostics;
        }

        #endregion Diagnostics
    }
}
