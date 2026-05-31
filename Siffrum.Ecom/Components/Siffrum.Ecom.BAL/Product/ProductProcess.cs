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
    public class ProductProcess : SiffrumBalOdataBase<ProductSM>
    {
        private readonly ILoginUserDetail _loginUserDetail;
        private readonly BrandProcess _brandProcess;
        private readonly CategoryProcess _categoryProcess;
        public ProductProcess(IMapper mapper, ApiDbContext apiDbContext, ILoginUserDetail loginUserDetail,
            BrandProcess brandProcess, CategoryProcess categoryProcess)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
            _brandProcess = brandProcess;
            _categoryProcess = categoryProcess;
        }

        #region OData
        public override async Task<IQueryable<ProductSM>> GetServiceModelEntitiesForOdata()
        {
            IQueryable<ProductDM> entitySet = _apiDbContext.Product.AsNoTracking();
            return await base.MapEntityAsToQuerable<ProductDM, ProductSM>(_mapper, entitySet);
        }
        #endregion

        #region Get All and Counts

        #region All

        public async Task<List<ProductSM>> GetAll(int skip, int top, PlatformTypeSM? platformType = null, bool includeInactive = false)
        {
            var query = _apiDbContext.Product.AsNoTracking().AsQueryable();
            if (!includeInactive)
                query = query.Where(x => x.ApprovalStatus != ProductStatusDM.Inactive);
            if (platformType.HasValue)
            {
                var platformDm = (PlatformTypeDM)(int)platformType.Value;
                query = query.Where(x => x.Category != null && x.Category.Platform == platformDm);
            }
            var dms = await query.OrderByDescending(x => x.Id).Skip(skip).Take(top).Select(x => x.Id).ToListAsync();
            var response = new List<ProductSM>();
            if(dms.Count == 0)
            {
                return response;
            }
            foreach (var id in dms)
            {
                var sm = await GetProductById(id);
                if(sm != null)
                {
                    response.Add(sm);
                }
                
            }
            return response;
        }

        public async Task<IntResponseRoot> GetAllCount(PlatformTypeSM? platformType = null, bool includeInactive = false)
        {
            var query = _apiDbContext.Product.AsNoTracking().AsQueryable();
            if (!includeInactive)
                query = query.Where(x => x.ApprovalStatus != ProductStatusDM.Inactive);
            if (platformType.HasValue)
            {
                var platformDm = (PlatformTypeDM)(int)platformType.Value;
                query = query.Where(x => x.Category != null && x.Category.Platform == platformDm);
            }
            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total Products");
        }

        public async Task<List<ProductSM>> GetAllSellerProducts(long sellerId, int skip, int top, int platformType = 0)
        {
            var query = _apiDbContext.Product.AsNoTracking()
                .Where(x => x.SellerId == sellerId
                    && x.ApprovalStatus != ProductStatusDM.Inactive
                    && x.ApprovalStatus != ProductStatusDM.Rejected);

            if (platformType > 0)
            {
                var pt = (PlatformTypeDM)platformType;
                query = query.Where(p =>
                    !_apiDbContext.ProductVariant.Any(v => v.ProductId == p.Id) ||
                    _apiDbContext.ProductVariant.Any(v => v.ProductId == p.Id && v.PlatformType == pt));
            }

            var dms = await query
                .OrderByDescending(x => x.Id)
                .Skip(skip).Take(top).Select(x => x.Id).ToListAsync();
            var response = new List<ProductSM>();
            if (dms.Count == 0)
            {
                return response;
            }
            foreach (var id in dms)
            {
                var sm = await GetProductById(id);
                if (sm != null)
                {
                    response.Add(sm);
                }

            }
            return response;
        }

        public async Task<IntResponseRoot> GetAllSellerProductsCount(long sellerId, int platformType = 0)
        {
            var query = _apiDbContext.Product.AsNoTracking()
                .Where(x => x.SellerId == sellerId
                    && x.ApprovalStatus != ProductStatusDM.Inactive
                    && x.ApprovalStatus != ProductStatusDM.Rejected);

            if (platformType > 0)
            {
                var pt = (PlatformTypeDM)platformType;
                query = query.Where(p => _apiDbContext.ProductVariant.Any(
                    v => v.ProductId == p.Id && v.PlatformType == pt));
            }

            var count = await query.CountAsync();
            return new IntResponseRoot(count, "Total Products");
        }

        public async Task<List<SearchResponseSM>> SearchProducts(
           long sellerId,
           string searchText,
           int skip,
           int top)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return new List<SearchResponseSM>();

            var words = searchText
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            IQueryable<ProductDM> query = _apiDbContext.Product
                .AsNoTracking();

            // Seller filter
            if (sellerId > 0)
            {
                query = query.Where(x => x.SellerId == sellerId);
            }

            // Multi-word search using LIKE (Better performance)
            foreach (var word in words)
            {
                query = query.Where(x => x.Name.ToLower().Contains(word.ToLower()));
            }

            return await query
                .OrderBy(x => x.Name)
                .Skip(skip)
                .Take(top)
                .Select(x => new SearchResponseSM
                {
                    Id = x.Id,
                    Title = x.Name
                })
                .ToListAsync();
        }

        #endregion All

        #endregion  Get All and Counts

        #region Get By Id

        public async Task<ProductSM> GetProductById(long id)
        {
            var dm = await _apiDbContext.Product.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if(dm == null)
            {
                return null;
            }
            var sm = _mapper.Map<ProductSM>(dm);
            var category = await _apiDbContext.Category.FindAsync(dm.CategoryId);
            var brand = dm.BrandId.HasValue ? await _apiDbContext.Brand.FindAsync(dm.BrandId.Value) : null;
            sm.CategoryName = category?.Name;
            sm.BrandName = brand?.Name;
            sm.ApprovalStatus = dm.ApprovalStatus.ToString();
            return sm;
        }

        #endregion Get By Id

        #region Add

        public async Task<ProductSM> AddProduct(long sellerId, ProductSM request)
        {
            if (request == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Product data is required");
            }
            if (string.IsNullOrEmpty(request.Name))
            {
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Product name is required");
            }
            if (request.BrandId.HasValue && request.BrandId.Value > 0)
            {
                var exisitngBrand = await _brandProcess.GetByIdAsync(request.BrandId.Value, "Seller");
                if (exisitngBrand == null)
                {
                    throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Brand not found");
                }
            }
            var existingCategory = await _categoryProcess.GetByIdAsync(request.CategoryId);
            if (existingCategory == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Category not found or invalid category");
            }
            var existingProduct = await GetSellerProductByName(sellerId, request.Name);
            if (existingProduct != null)
            {
                return existingProduct;
            }
            var existingWithSlug = await GetSellerProductBySlug(request.Slug);
            if (existingWithSlug != null)
            {
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Product slug already exists");
            }

            var dm = _mapper.Map<ProductDM>(request);
            dm.SellerId = sellerId;
            dm.ApprovalStatus = ProductStatusDM.PendingApproval;
            _apiDbContext.Product.Add(dm);
            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                return await GetProductById(dm.Id);
            }

            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log,
               $"Error in adding product by Seller with SellerId:{sellerId}"
               , "Something went wrong while adding product details. Please try again.");
        }


        #endregion Add

        #region Update Product Status

        public async Task<BoolResponseRoot> UpdateProduct(long sellerId, long id, ProductSM objSM)
        {
            var dm = await _apiDbContext.Product.FindAsync(id);
            if (dm == null)
            {
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Product not found");
            }
            if(dm.SellerId != sellerId)
            {
                throw new SiffrumException(ApiErrorTypeSM.Access_Denied_Log,
                    $"Seller with Id: {sellerId} tries to update product id: {dm.Id} which is not product of this seller", 
                    "You are not authorized to update this product");
            }
            if (!string.Equals(dm.Name, objSM.Name, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _apiDbContext.Product
                    .AsNoTracking()
                    .AnyAsync(x => x.Id != id && x.SellerId == dm.SellerId && x.Name == objSM.Name);

                if (exists)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Product name already exists"
                    );
            }

            if (!string.Equals(dm.Slug, objSM.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _apiDbContext.Product
                    .AsNoTracking()
                    .AnyAsync(x => x.Id != id && x.Slug == objSM.Slug);

                if (exists)
                    throw new SiffrumException(
                        ApiErrorTypeSM.InvalidInputData_NoLog,
                        "Product slug already exists"
                    );
            }
            if(objSM.CategoryId != dm.CategoryId)
            {
                var exisitingCategory = await _categoryProcess.GetByIdAsync(objSM.CategoryId);
                if(exisitingCategory == null)
                {
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Category not found");
                }
                if(exisitingCategory.Level == 1 || exisitingCategory.Status == StatusSM.Inactive)
                {
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Invalid category");
                }
                dm.CategoryId = objSM.CategoryId;
            }

            if (objSM.BrandId != dm.BrandId)
            {
                if (objSM.BrandId.HasValue && objSM.BrandId.Value > 0)
                {
                    var existingBrand = await _brandProcess.GetByIdAsync(objSM.BrandId.Value, "Seller");
                    if (existingBrand == null)
                    {
                        throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Brand not found");
                    }
                }
                dm.BrandId = objSM.BrandId;
            }
            dm.Name = objSM.Name;
            dm.Slug = objSM.Slug;
            dm.TaxPercentage = objSM.TaxPercentage;
            dm.Tags = objSM.Tags;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;
            if (await _apiDbContext.SaveChangesAsync() > 0)
            {
                return new BoolResponseRoot(true, "Product updated successfully");
            }
            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, $"Product with Id:{id} updation failed", "Failed to update product details");
        }

        #endregion Update

        #region Delete Product

        public async Task<DeleteResponseRoot> DeleteProduct(long sellerId, long id)
        {
            var product = await _apiDbContext.Product
                    .FirstOrDefaultAsync(x => x.Id == id && x.SellerId == sellerId);

            if (product == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    "Product not found"
                );
            }

            var variants = await _apiDbContext.ProductVariant.AnyAsync(x => x.ProductId == id); 
            if (variants)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    "Product has variants and cannot be deleted"
                );
            }

            // Remove related toppings
            var toppings = await _apiDbContext.ProductTopping.Where(t => t.ProductId == id).ToListAsync();
            if (toppings.Any()) _apiDbContext.ProductTopping.RemoveRange(toppings);

            _apiDbContext.Product.Remove(product);

            await _apiDbContext.SaveChangesAsync();

            return new DeleteResponseRoot(true, "Product deleted successfully");
        }

        public async Task<DeleteResponseRoot> DeleteProductByAdmin( long id)
        {
            var product = await _apiDbContext.Product
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    "Product not found"
                );
            }

            var variants = await _apiDbContext.ProductVariant.AnyAsync(x => x.ProductId == id);
            if (variants)
            {
                throw new SiffrumException(
                    ApiErrorTypeSM.InvalidInputData_Log,
                    "Product has variants and cannot be deleted"
                );
            }

            // 🔹 Remove related toppings
            var toppings = await _apiDbContext.ProductTopping.Where(t => t.ProductId == id).ToListAsync();
            if (toppings.Any()) _apiDbContext.ProductTopping.RemoveRange(toppings);

            // 🔹 Remove DB records
            _apiDbContext.Product.Remove(product);

            await _apiDbContext.SaveChangesAsync();

            return new DeleteResponseRoot(true, "Product deleted successfully");
        }


        /// <summary>
        /// Atomically delete a product AND all its variants, cleaning up FK deps.
        /// Used by admin "remove seller" flow.
        /// </summary>
        public async Task<DeleteResponseRoot> DeleteProductWithVariantsByAdmin(long productId)
        {
            var product = await _apiDbContext.Product.FirstOrDefaultAsync(x => x.Id == productId);
            if (product == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Product not found");

            var variantIds = await _apiDbContext.ProductVariant
                .Where(v => v.ProductId == productId)
                .Select(v => v.Id)
                .ToListAsync();

            // Check if any variant has order history
            var hasOrders = variantIds.Any() && await _apiDbContext.OrderItem
                .AnyAsync(oi => variantIds.Contains(oi.ProductVariantId));

            if (hasOrders)
            {
                // Soft-delete: deactivate product + variants instead of hard delete
                var variants = await _apiDbContext.ProductVariant
                    .Where(v => v.ProductId == productId)
                    .ToListAsync();
                foreach (var v in variants)
                    v.Status = ProductStatusDM.Inactive;

                product.ApprovalStatus = ProductStatusDM.Inactive;
                await _apiDbContext.SaveChangesAsync();

                return new DeleteResponseRoot(true, "Product deactivated (has order history)");
            }

            await using var tx = await _apiDbContext.Database.BeginTransactionAsync();
            try
            {
                if (variantIds.Any())
                {
                    // addon_products (Restrict FK — must delete manually)
                    var addons = await _apiDbContext.AddonProducts
                        .Where(a => variantIds.Contains(a.MainProductId) || variantIds.Contains(a.AddonProductId))
                        .ToListAsync();
                    if (addons.Any()) _apiDbContext.AddonProducts.RemoveRange(addons);

                    // Delete all variants
                    var variants = await _apiDbContext.ProductVariant
                        .Where(v => v.ProductId == productId)
                        .ToListAsync();
                    _apiDbContext.ProductVariant.RemoveRange(variants);
                }

                // product_toppings (Cascade FK but clean up explicitly for safety)
                var toppings = await _apiDbContext.ProductTopping
                    .Where(t => t.ProductId == productId).ToListAsync();
                if (toppings.Any()) _apiDbContext.ProductTopping.RemoveRange(toppings);

                _apiDbContext.Product.Remove(product);

                await _apiDbContext.SaveChangesAsync();
                await tx.CommitAsync();

                return new DeleteResponseRoot(true, "Product and variants deleted successfully");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion Delete Product

        #region Product Already Present By Seller

        public async Task<ProductSM> GetSellerProductByName(long sellerId, string name)
        {
            var dm = await _apiDbContext.Product.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower() && x.SellerId == sellerId);
            if(dm != null)
            {
                var sm = _mapper.Map<ProductSM>(dm);
                return sm;
            }
            return null;
            
        }
        public async Task<ProductSM> GetSellerProductBySlug( string slug)
        {
            var dm = await _apiDbContext.Product.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug.ToLower() == slug.ToLower());
            if(dm != null)
            {
                var sm = _mapper.Map<ProductSM>(dm);
                return sm;
            }
            return null;
            
        }

        #endregion Product Already Present By Seller

        #region Admin Product Management

        public async Task<List<ProductSM>> AddProductByAdmin(AdminProductCreateSM request)
        {
            if (request?.Product == null)
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Product data is required");

            if (string.IsNullOrEmpty(request.Product.Name))
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Product name is required");

            if (request.SellerIds == null || request.SellerIds.Count == 0)
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "At least one seller must be selected");

            var existingCategory = await _categoryProcess.GetByIdAsync(request.Product.CategoryId);
            if (existingCategory == null)
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Category not found");

            var results = new List<ProductSM>();

            foreach (var sellerId in request.SellerIds)
            {
                var seller = await _apiDbContext.Seller.FindAsync(sellerId);
                if (seller == null) continue;

                var existingProduct = await GetSellerProductByName(sellerId, request.Product.Name);
                if (existingProduct != null)
                {
                    results.Add(existingProduct);
                    continue;
                }

                var slug = request.Product.Slug;
                if (!string.IsNullOrEmpty(slug) && request.SellerIds.Count > 1)
                {
                    slug = $"{slug}-{sellerId}";
                }

                var dm = _mapper.Map<ProductDM>(request.Product);
                dm.SellerId = sellerId;
                dm.Slug = slug;
                dm.CreatedAt = DateTime.UtcNow;
                dm.CreatedBy = _loginUserDetail.LoginId;
                _apiDbContext.Product.Add(dm);

                if (await _apiDbContext.SaveChangesAsync() > 0)
                {
                    var created = await GetProductById(dm.Id);
                    if (created != null) results.Add(created);
                }
            }

            if (results.Count == 0)
                throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, "Failed to create product for any seller");

            return results;
        }

        public async Task<BoolResponseRoot> UpdateProductByAdmin(long id, ProductSM objSM)
        {
            var dm = await _apiDbContext.Product.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Product not found");

            if (!string.Equals(dm.Name, objSM.Name, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _apiDbContext.Product
                    .AsNoTracking()
                    .AnyAsync(x => x.Id != id && x.SellerId == dm.SellerId && x.Name == objSM.Name);
                if (exists)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Product name already exists for this seller");
            }

            if (!string.Equals(dm.Slug, objSM.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _apiDbContext.Product
                    .AsNoTracking()
                    .AnyAsync(x => x.Id != id && x.Slug == objSM.Slug);
                if (exists)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Product slug already exists");
            }

            if (objSM.CategoryId != dm.CategoryId)
            {
                var cat = await _categoryProcess.GetByIdAsync(objSM.CategoryId);
                if (cat == null)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Category not found");
                dm.CategoryId = objSM.CategoryId;
            }

            if (objSM.SellerId > 0 && objSM.SellerId != dm.SellerId)
            {
                var seller = await _apiDbContext.Seller.FindAsync(objSM.SellerId);
                if (seller == null)
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Seller not found");
                dm.SellerId = objSM.SellerId;
            }

            dm.Name = objSM.Name;
            dm.Slug = objSM.Slug;
            dm.TaxPercentage = objSM.TaxPercentage;
            dm.BrandId = objSM.BrandId;
            dm.Tags = objSM.Tags;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            if (await _apiDbContext.SaveChangesAsync() > 0)
                return new BoolResponseRoot(true, "Product updated successfully");

            throw new SiffrumException(ApiErrorTypeSM.Fatal_Log, "Failed to update product");
        }

        public async Task<BoolResponseRoot> ApproveProduct(long id)
        {
            var dm = await _apiDbContext.Product.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Product not found");

            dm.ApprovalStatus = ProductStatusDM.Active;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            var pendingVariants = await _apiDbContext.ProductVariant
                .Where(v => v.ProductId == id && v.Status == ProductStatusDM.PendingApproval)
                .ToListAsync();
            foreach (var v in pendingVariants)
            {
                v.Status = ProductStatusDM.Active;
                v.UpdatedAt = DateTime.UtcNow;
            }

            await _apiDbContext.SaveChangesAsync();
            return new BoolResponseRoot(true, "Product approved successfully");
        }

        public async Task<BoolResponseRoot> RejectProduct(long id)
        {
            var dm = await _apiDbContext.Product.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Product not found");

            dm.ApprovalStatus = ProductStatusDM.Rejected;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            var pendingVariants = await _apiDbContext.ProductVariant
                .Where(v => v.ProductId == id && v.Status == ProductStatusDM.PendingApproval)
                .ToListAsync();
            foreach (var v in pendingVariants)
            {
                v.Status = ProductStatusDM.Rejected;
                v.UpdatedAt = DateTime.UtcNow;
            }

            await _apiDbContext.SaveChangesAsync();
            return new BoolResponseRoot(true, "Product rejected");
        }

        public async Task<BoolResponseRoot> SetProductStatus(long id, ProductStatusSM status)
        {
            var dm = await _apiDbContext.Product.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_Log, "Product not found");

            dm.ApprovalStatus = (ProductStatusDM)(int)status;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;
            await _apiDbContext.SaveChangesAsync();
            return new BoolResponseRoot(true, $"Product status set to {status}");
        }

        #endregion Admin Product Management

        #region Cleanup

        /// <summary>
        /// One-time cleanup: finds duplicate products per seller (same Name + SellerId),
        /// merges all variants into the oldest product, deletes the empty duplicates.
        /// Returns summary of what was cleaned.
        /// </summary>
        public async Task<BoolResponseRoot> CleanupDuplicateProducts()
        {
            // Find (SellerId, LowerName) groups with more than 1 product
            var duplicateGroups = await _apiDbContext.Product
                .AsNoTracking()
                .GroupBy(p => new { p.SellerId, LowerName = p.Name.ToLower().Trim() })
                .Where(g => g.Count() > 1)
                .Select(g => new { g.Key.SellerId, g.Key.LowerName })
                .ToListAsync();

            int mergedProducts = 0;
            int movedVariants = 0;
            int deletedProducts = 0;

            foreach (var dup in duplicateGroups)
            {
                // Get all product IDs in this group, ordered by ID (keep the oldest)
                var productIds = await _apiDbContext.Product
                    .AsNoTracking()
                    .Where(p => p.SellerId == dup.SellerId && p.Name.ToLower().Trim() == dup.LowerName)
                    .OrderBy(p => p.Id)
                    .Select(p => p.Id)
                    .ToListAsync();

                if (productIds.Count <= 1)
                    continue;

                var keepProductId = productIds[0]; // oldest
                var removeProductIds = productIds.Skip(1).ToList();
                mergedProducts++;

                // Move variants from duplicate products to the keep product
                foreach (var removeId in removeProductIds)
                {
                    var variants = await _apiDbContext.ProductVariant
                        .Where(v => v.ProductId == removeId)
                        .ToListAsync();

                    foreach (var v in variants)
                    {
                        // Check if the keep product already has a variant with same name
                        var existsInKeep = await _apiDbContext.ProductVariant
                            .AsNoTracking()
                            .AnyAsync(x => x.ProductId == keepProductId &&
                                           x.Name.ToLower().Trim() == v.Name.ToLower().Trim());

                        if (existsInKeep)
                        {
                            // Duplicate variant — remove it
                            _apiDbContext.ProductVariant.Remove(v);
                        }
                        else
                        {
                            // Move to the keep product
                            v.ProductId = keepProductId;
                            movedVariants++;
                        }
                    }

                    await _apiDbContext.SaveChangesAsync();

                    // Delete the now-empty product
                    var emptyProduct = await _apiDbContext.Product.FindAsync(removeId);
                    if (emptyProduct != null)
                    {
                        // Double-check no variants remain
                        var remainingCount = await _apiDbContext.ProductVariant
                            .CountAsync(v => v.ProductId == removeId);
                        if (remainingCount == 0)
                        {
                            _apiDbContext.Product.Remove(emptyProduct);
                            await _apiDbContext.SaveChangesAsync();
                            deletedProducts++;
                        }
                    }
                }
            }

            var msg = $"Cleanup complete: {duplicateGroups.Count} duplicate groups found, " +
                      $"{movedVariants} variants moved, {deletedProducts} empty products deleted.";
            return new BoolResponseRoot(true, msg);
        }

        #endregion Cleanup

    }
}
