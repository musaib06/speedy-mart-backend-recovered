using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Base.ImageProcess;
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
    public class ProductRatingProcess : SiffrumBalOdataBase<ProductRatingSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly ImageProcess  _imageProcess;
        public ProductRatingProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail, ImageProcess imageProcess)
            :base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _imageProcess = imageProcess;
        }

        #region OData
        public override async Task<IQueryable<ProductRatingSM>> GetServiceModelEntitiesForOdata()
        {
            IQueryable<ProductRatingDM> entitySet = _apiDbContext.ProductRating.AsNoTracking();
            return await base.MapEntityAsToQuerable<ProductRatingDM, ProductRatingSM>(_mapper, entitySet);
        }
        #endregion

        #region Get All and Counts

        #region All

        public async Task<List<ProductRatingSM>> GetAll(int skip, int top)
        {
            var dms = await _apiDbContext.ProductRating.AsNoTracking().Skip(skip).Take(top).ToListAsync();
            var sms = _mapper.Map<List<ProductRatingSM>>(dms);
            return sms;
        }

        public async Task<IntResponseRoot> GetAllCount()
        {
            var count = await _apiDbContext.ProductRating.AsNoTracking().CountAsync();
            return new IntResponseRoot(count, "Total Product Ratings");
        }
        #endregion All

        #region Product Ratings

        public async Task<List<ProductRatingSM>> GetAllProductRatings(long productVariantId, int skip, int top)
        {
            var ids = await _apiDbContext.ProductRating.AsNoTracking()
                .Where(x => x.ProductVariantId == productVariantId && x.Status == StatusDM.Active)
                .Skip(skip).Take(top)                
                .Select(x => x.Id)
                .ToListAsync();
            var response = new List<ProductRatingSM>();
            if (ids.Count == 0) return response;
            foreach (var id in ids)
            {
                var res = await GetProductRatingById(id);
                if (res != null)
                    response.Add(res);
            }
            return response;
        }

        public async Task<IntResponseRoot> GetAllProductRatingsCount(long productVariantId)
        {
            var count = await _apiDbContext.ProductRating.AsNoTracking()
                .Where(x=>x.ProductVariantId == productVariantId && x.Status == StatusDM.Active)
                .CountAsync();
            return new IntResponseRoot(count, "Total Product Ratings");
        }

        #endregion Product Ratings

        #endregion  Get All and Counts

        #region Get By Id

        public async Task<ProductRatingSM> GetProductRatingById(long id) 
        {
            var dm = await _apiDbContext.ProductRating.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (dm == null) return null;
            var role = _loginUserDetail.UserType;
            var currentUserId = _loginUserDetail.DbRecordId;
            // Pending reviews: visible only to the author user
            if (dm.Status != StatusDM.Active)
            {
                if (role == RoleTypeSM.User && dm.UserId != currentUserId)
                    return null;
            }
            return await MapRatingToSM(dm);
        }      

        private async Task<ProductRatingSM> MapRatingToSM(ProductRatingDM dm)
        {
            var sm = _mapper.Map<ProductRatingSM>(dm);
            var usersName = await _apiDbContext.User.AsNoTracking().Where(x => x.Id == dm.UserId).Select(x => x.Name).FirstOrDefaultAsync();
            sm.UserName = string.IsNullOrEmpty(usersName) ? "Anonymous" : usersName;
            var images = await _apiDbContext.RatingImages.AsNoTracking().Where(x => x.ProductRatingId == dm.Id).ToListAsync();
            var imageBase64List = new List<string>();
            var networkImageList = new List<string>();
            foreach (var image in images)
            {
                var resolved = await _imageProcess.ResolveImage(image.Image);
                if (!string.IsNullOrEmpty(resolved.Base64))
                    imageBase64List.Add(resolved.Base64);
                if (!string.IsNullOrEmpty(resolved.NetworkUrl))
                    networkImageList.Add(resolved.NetworkUrl);
            }
            sm.Images = imageBase64List;
            sm.NetworkImages = networkImageList;
            return sm;
        }

        #endregion Get By Id

        #region Add

        public async Task<ProductRatingSM> AddProductRating(ProductRatingSM request)
        {
            using var transaction = await _apiDbContext.Database.BeginTransactionAsync();

            try
            {
                // 🔹 Validate product variant
                var variantExists = await _apiDbContext.ProductVariant
                    .AnyAsync(x => x.Id == request.ProductVariantId && x.Status == ProductStatusDM.Active);

                if (!variantExists)
                {
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_Log,
                        $"User tried to rate non-active product variant with Id: {request.ProductVariantId}",
                        "Product not found"
                    );
                }

                // 🔹 Verify user has purchased and received this product (Delivered order)
                var hasPurchased = await _apiDbContext.OrderItem.AsNoTracking()
                    .AnyAsync(oi => oi.ProductVariantId == request.ProductVariantId
                        && oi.Order.UserId == request.UserId
                        && oi.Order.OrderStatus == OrderStatusDM.Delivered);

                if (!hasPurchased)
                {
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "You can only review products you have purchased and received."
                    );
                }

                var alreadyRated = await _apiDbContext.ProductRating
                    .AnyAsync(x =>
                        x.ProductVariantId == request.ProductVariantId &&
                        x.UserId == request.UserId);

                if (alreadyRated)
                {
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_Log,
                        "You have already rated this product, Thank you for your feedback"
                    );
                }

                // 🔹 Validate rate: 1-5 stars
                if (request.Rate < 1 || request.Rate > 5)
                {
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Rating must be between 1 and 5 stars."
                    );
                }

                // 🔹 Limit images to max 3
                if (request.Images != null && request.Images.Count > 3)
                {
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "You can upload a maximum of 3 images per review."
                    );
                }

                // 🔹 Create rating — starts as Pending, seller must approve
                var rating = new ProductRatingDM
                {
                    ProductVariantId = request.ProductVariantId,
                    UserId = request.UserId,
                    Rate = request.Rate,
                    Review = request.Review,
                    Status = StatusDM.Pending,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _loginUserDetail.LoginId
                };

                _apiDbContext.ProductRating.Add(rating);
                await _apiDbContext.SaveChangesAsync();

                // 🔹 Save images (max 3)
                if (request.Images != null && request.Images.Count > 0)
                {
                    foreach (var base64 in request.Images.Take(3))
                    {
                        var imagePath = await _imageProcess.SaveFromBase64(
                            base64,
                            "jpg",
                            @"wwwroot/content/ratings"
                        );

                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            _apiDbContext.RatingImages.Add(new RatingImagesDM
                            {
                                ProductRatingId = rating.Id,
                                Image = imagePath
                            });
                        }
                    }

                    await _apiDbContext.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return await MapRatingToSM(rating);
            }
            catch (SiffrumException) { await transaction.RollbackAsync(); throw; }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, 
                    $"Error in adding product rating by User with UserId:{request.UserId} to ProductVariantId:{request.ProductVariantId}, Reason: {ex.Message}, Inner Exception: {ex.InnerException}, Stack Trace: {ex.StackTrace}"
                    ,"Something went wrong while adding product rating. Please try again.");
            }
        }


        #endregion Add

        #region Update Rating Status

        public async Task<BoolResponseRoot> UpdateProductRatingStatus(long id, StatusSM status)
        {
            var existingRating = await _apiDbContext.ProductRating.FindAsync(id);
            if (existingRating == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Product Rating not found");
            }
            if(existingRating.Status == (StatusDM)status)
            {
                return new BoolResponseRoot(false, $"Product Rating Status already updated to {status.ToString()}");
            }
            existingRating.Status = (StatusDM)status;
            existingRating.UpdatedAt = DateTime.UtcNow;
            existingRating.UpdatedBy = _loginUserDetail.LoginId;
            if(await _apiDbContext.SaveChangesAsync()>0)
            {
                return new BoolResponseRoot(true, "Product Rating Status updated successfully");
            }
            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"Product Rating with Id:{id} status updation failed to {status.ToString()}","Failed to update product rating");
        }

        #endregion Update

        #region Delete Rating

        public async Task<DeleteResponseRoot> DeleteProductRating(long id)
        {
            using var transaction = await _apiDbContext.Database.BeginTransactionAsync();

            try
            {
                var rating = await _apiDbContext.ProductRating
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (rating == null)
                {
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_Log,
                        "Product rating not found"
                    );
                }

                var images = await _apiDbContext.RatingImages
                    .Where(x => x.ProductRatingId == id)
                    .ToListAsync();

                var imagePaths = images
                    .Where(x => !string.IsNullOrEmpty(x.Image))
                    .Select(x => x.Image!)
                    .ToList();

                // 🔹 Remove DB records
                _apiDbContext.RatingImages.RemoveRange(images);
                _apiDbContext.ProductRating.Remove(rating);

                await _apiDbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 🔹 Delete physical files AFTER commit
                foreach (var path in imagePaths)
                {
                    if (File.Exists(path))
                        File.Delete(path);                   
                }

                return new DeleteResponseRoot(true, "Product rating deleted successfully");
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync();
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"Failed to delete product rating: {ex.Message}, InnerException: {ex.InnerException}, StackTrace: {ex.StackTrace}", "Failed to delete product rating");
            }
        }


        #endregion Delete Rating

        #region Seller Review Management

        /// <summary>
        /// Get reviews for the seller's own products (SpeedyMart).
        /// Seller can see all statuses (Pending, Active, Inactive).
        /// </summary>
        public async Task<List<ProductRatingSM>> GetSellerReviews(
            long sellerId, int skip, int top, StatusSM? status = null)
        {
            var query = _apiDbContext.ProductRating.AsNoTracking()
                .Where(r => _apiDbContext.ProductVariant.Any(
                    pv => pv.Id == r.ProductVariantId
                       && pv.Product.SellerId == sellerId));

            if (status.HasValue)
                query = query.Where(r => r.Status == (StatusDM)status.Value);

            var dms = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            var result = new List<ProductRatingSM>();
            foreach (var dm in dms)
            {
                var sm = await MapRatingToSM(dm);
                result.Add(sm);
            }
            return result;
        }

        public async Task<IntResponseRoot> GetSellerReviewsCount(
            long sellerId, StatusSM? status = null)
        {
            var query = _apiDbContext.ProductRating.AsNoTracking()
                .Where(r => _apiDbContext.ProductVariant.Any(
                    pv => pv.Id == r.ProductVariantId
                       && pv.Product.SellerId == sellerId));

            if (status.HasValue)
                query = query.Where(r => r.Status == (StatusDM)status.Value);

            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total Seller Reviews");
        }

        /// <summary>
        /// Seller approves or rejects a review (must belong to seller's product).
        /// </summary>
        public async Task<BoolResponseRoot> SellerUpdateReviewStatus(
            long ratingId, long sellerId, StatusSM newStatus)
        {
            var rating = await _apiDbContext.ProductRating.FindAsync(ratingId);
            if (rating == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Review not found");

            // Verify this review belongs to seller's product
            var belongsToSeller = await _apiDbContext.ProductVariant.AsNoTracking()
                .AnyAsync(pv => pv.Id == rating.ProductVariantId
                             && pv.Product.SellerId == sellerId);

            if (!belongsToSeller)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "This review does not belong to your product");

            if (rating.Status == (StatusDM)newStatus)
                return new BoolResponseRoot(false, $"Review status is already {newStatus}");

            rating.Status = (StatusDM)newStatus;
            rating.UpdatedAt = DateTime.UtcNow;
            rating.UpdatedBy = _loginUserDetail.LoginId;

            if (await _apiDbContext.SaveChangesAsync() > 0)
                return new BoolResponseRoot(true, $"Review {(newStatus == StatusSM.Active ? "approved" : "rejected")} successfully");

            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log,
                $"Failed to update review status for ratingId={ratingId}",
                "Failed to update review status");
        }

        #endregion Seller Review Management

        #region User Review Methods

        /// <summary>
        /// Get current user's own reviews (all statuses — user can see their pending reviews).
        /// </summary>
        public async Task<List<ProductRatingSM>> GetMyReviews(long userId, int skip, int top)
        {
            var dms = await _apiDbContext.ProductRating.AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            var result = new List<ProductRatingSM>();
            foreach (var dm in dms)
            {
                var sm = await MapRatingToSM(dm);
                result.Add(sm);
            }
            return result;
        }

        /// <summary>
        /// Check if user can review a product variant (has purchased + not already reviewed).
        /// </summary>
        public async Task<BoolResponseRoot> CanUserReview(long userId, long productVariantId)
        {
            var hasPurchased = await _apiDbContext.OrderItem.AsNoTracking()
                .AnyAsync(oi => oi.ProductVariantId == productVariantId
                    && oi.Order.UserId == userId
                    && oi.Order.OrderStatus == OrderStatusDM.Delivered);

            if (!hasPurchased)
                return new BoolResponseRoot(false, "You have not purchased this product yet.");

            var alreadyReviewed = await _apiDbContext.ProductRating.AsNoTracking()
                .AnyAsync(r => r.ProductVariantId == productVariantId && r.UserId == userId);

            if (alreadyReviewed)
                return new BoolResponseRoot(false, "You have already reviewed this product.");

            return new BoolResponseRoot(true, "You can review this product.");
        }

        #endregion User Review Methods

    }
}
