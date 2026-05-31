using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Siffrum.Ecom.BAL.Foundation.Web;
using Siffrum.Ecom.BAL.Product;
using Siffrum.Ecom.Foundation.Controllers.Base;
using Siffrum.Ecom.Foundation.Security;
using Siffrum.Ecom.ServiceModels.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.v1;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.DAL.Context;
using System.Text.Json;

namespace Siffrum.Ecom.Foundation.Controllers.Product.ProductControllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ProductController : ApiControllerWithOdataRoot<ProductSM>
    {
        private readonly ProductProcess _productProcess;
        private readonly ProductVariantProcess _productVariantProcess;
        private readonly ProductAttributeDimensionProcess _attrDimProcess;
        private readonly ProductImagesProcess _productImagesProcess;
        private readonly ProductSpecificationProcess _productSpecificationProcess;
        private readonly ApiDbContext _apiDbContext;

        public ProductController(
            ProductProcess process,
            ProductVariantProcess productVariantProcess,
            ProductAttributeDimensionProcess attrDimProcess,
            ProductImagesProcess productImagesProcess,
            ProductSpecificationProcess productSpecificationProcess,
            ApiDbContext apiDbContext)
            : base(process)
        {
            _productProcess = process;
            _productVariantProcess = productVariantProcess;
            _attrDimProcess = attrDimProcess;
            _productImagesProcess = productImagesProcess;
            _productSpecificationProcess = productSpecificationProcess;
            _apiDbContext = apiDbContext;
        }

        #region ODATA
        [HttpGet("odata")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductSM>>>> GetAsOdata(
            ODataQueryOptions<ProductSM> oDataOptions)
        {
            var retList = await GetAsEntitiesOdata(oDataOptions);
            return Ok(ModelConverter.FormNewSuccessResponse(retList));
        }
        #endregion

        #region Get

        #region GetAll
        [HttpGet]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ProductSM>>>> GetAll(
            int skip, int top, PlatformTypeSM? platformType = null, bool includeInactive = false)
        {
            var response = await _productProcess.GetAll(skip, top, platformType, includeInactive);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("count")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetAllCount(PlatformTypeSM? platformType = null, bool includeInactive = false)
        {
            var response = await _productProcess.GetAllCount(platformType, includeInactive);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("mine")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<List<ProductSM>>>> GetAllMine(
            int skip, int top, int platformType = 0)
        {
            #region Check Request

            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }

            #endregion Check Request

            var response = await _productProcess.GetAllSellerProducts(userId, skip, top, platformType);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("mine/count")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetAllMineCount(int platformType = 0)
        {
            #region Check Request

            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }

            #endregion Check Request
            var response = await _productProcess.GetAllSellerProductsCount(userId, platformType);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("seller/{sellerId}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ProductSM>>>> GetAllSellerProducts(long sellerId,
            int skip, int top, int platformType = 0)
        {
            #region Check Request



            #endregion Check Request

            var response = await _productProcess.GetAllSellerProducts(sellerId, skip, top, platformType);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("seller/count/{sellerId}")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetAllSellerProductsCount(long sellerId, int platformType = 0)
        {
            #region Check Request
            #endregion Check Request
            var response = await _productProcess.GetAllSellerProductsCount(sellerId, platformType);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("search")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin, Seller, User")]
        public async Task<ActionResult<ApiResponse<List<SearchResponseSM>>>> GetAllBySearch(string searchString,
            int skip = 0, int top = 50)
        {
            long sellerId = 0;
            var role = User.GetUserRoleTypeFromCurrentUserClaims();
            if (role == RoleTypeSM.Seller.ToString())
            {
                sellerId = User.GetUserRecordIdFromCurrentUserClaims();
                if (sellerId <= 0)
                {
                    return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
                }
            }
            var response = await _productProcess.SearchProducts(sellerId, searchString, skip, top);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion GetAll

        #region Get By Id

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin, User, Seller")]
        public async Task<ActionResult<ApiResponse<ProductSM>>> GetById(long id)
        {
            var response = await _productProcess.GetProductById(id);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion Get By Id

        #endregion Get

        #region Add
        [HttpPost("mine")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<ProductSM>>> Add([FromBody] ApiRequest<ProductSM> apiRequest)
        {
            #region Check Request 
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request
            var response = await _productProcess.AddProduct(userId, innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion

        #region Update

        [HttpPut("mine")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Update(long id, [FromBody] ApiRequest<ProductSM> apiRequest)
        {
            #region Check Request 
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed, ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_Id_NotFound));
            }
            #endregion Check Request
            var response = await _productProcess.UpdateProduct(userId,id, innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion Update

        #region Delete
        [HttpDelete("mine/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> Delete(long id)
        {
            #region Check Request

            var userId = User.GetUserRecordIdFromCurrentUserClaims();
            if (userId <= 0)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstantsRoot.DisplayMessagesRoot.Display_Id_NotFound));
            }

            #endregion Check Request
            var response = await _productProcess.DeleteProduct(userId, id);
            return ModelConverter.FormNewSuccessResponse(response);
        }
        
        [HttpDelete("admin/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> DeleteByAdmin(long id)
        {
            #region Check Request


            #endregion Check Request
            var response = await _productProcess.DeleteProductByAdmin(id);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpDelete("admin/full/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<DeleteResponseRoot>>> DeleteWithVariantsByAdmin(long id)
        {
            var response = await _productProcess.DeleteProductWithVariantsByAdmin(id);
            return ModelConverter.FormNewSuccessResponse(response);
        }
        #endregion

        #region Admin Approval Queue

        [HttpGet("admin/pending")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ProductSM>>>> GetPending(
            int skip = 0, int top = 20, PlatformTypeSM? platformType = null)
        {
            var response = await _productProcess.GetPendingProducts(skip, top, platformType);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpGet("admin/pending/count")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<IntResponseRoot>>> GetPendingCount(PlatformTypeSM? platformType = null)
        {
            var response = await _productProcess.GetPendingProductsCount(platformType);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion Admin Approval Queue

        #region Admin Create / Update

        [HttpPost("admin")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<List<ProductSM>>>> AddByAdmin(
            [FromBody] ApiRequest<AdminProductCreateSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var response = await _productProcess.AddProductByAdmin(innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPut("admin/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> UpdateByAdmin(
            long id, [FromBody] ApiRequest<ProductSM> apiRequest)
        {
            var innerReq = apiRequest?.ReqData;
            if (innerReq == null)
            {
                return BadRequest(ModelConverter.FormNewErrorResponse(
                    DomainConstantsRoot.DisplayMessagesRoot.Display_ReqDataNotFormed,
                    ApiErrorTypeSM.InvalidInputData_NoLog));
            }
            var response = await _productProcess.UpdateProductByAdmin(id, innerReq);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPut("admin/approve/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> ApproveProduct(long id)
        {
            var response = await _productProcess.ApproveProduct(id);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPut("admin/reject/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> RejectProduct(long id)
        {
            var response = await _productProcess.RejectProduct(id);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPut("admin/status/{id}")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SetProductStatus(long id, ProductStatusSM status)
        {
            var response = await _productProcess.SetProductStatus(id, status);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion Admin Create / Update

        #region PRODUCT OVERVIEW POINTS

        [HttpGet("{id}/overview")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<List<string>>>> GetOverviewPoints(long id)
        {
            var prod = await _productProcess.GetProductById(id);
            var json = prod?.OverviewPoints;
            List<string> points;
            try { points = string.IsNullOrWhiteSpace(json) ? new List<string>() : System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
            catch { points = new List<string>(); }
            return ModelConverter.FormNewSuccessResponse(points);
        }

        [HttpPut("{id}/overview")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin, SystemAdmin, Seller")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> UpdateOverviewPoints(long id, [FromBody] ApiRequest<List<string>> apiRequest)
        {
            var points = apiRequest?.ReqData ?? new List<string>();
            var json = System.Text.Json.JsonSerializer.Serialize(points.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList());
            var resp = await _productProcess.UpdateOverviewPointsAsync(id, json);
            return ModelConverter.FormNewSuccessResponse(resp);
        }

        #endregion PRODUCT OVERVIEW POINTS

        #region Cleanup

        [HttpPost("cleanup-duplicates")]
        [Authorize(AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema, Roles = "SuperAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> CleanupDuplicateProducts()
        {
            var response = await _productProcess.CleanupDuplicateProducts();
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion Cleanup

        #region PRODUCT APPROVAL WORKFLOW

        [HttpPost("{id}/submit-for-approval")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "Seller")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> SubmitForApproval(long id, long sellerId)
        {
            var response = await _productProcess.SubmitForApprovalAsync(id, sellerId);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPut("{id}/approve")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Approve(long id)
        {
            var response = await _productProcess.ApproveProductAsync(id);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        [HttpPut("{id}/reject")]
        [Authorize(
            AuthenticationSchemes = SiffrumBearerTokenAuthHandlerRoot.DefaultSchema,
            Roles = "SuperAdmin, SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BoolResponseRoot>>> Reject(long id, [FromQuery] string rejectionReason)
        {
            var response = await _productProcess.RejectProductAsync(id, rejectionReason);
            return ModelConverter.FormNewSuccessResponse(response);
        }

        #endregion PRODUCT APPROVAL WORKFLOW

        #region SPEEDYMART PDP AGGREGATION

        [HttpGet("{id}/pdp")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<SpeedyMartPdpSM>>> GetPdp(long id)
        {
            // Product
            var product = await _productProcess.GetProductById(id);
            if (product == null)
            {
                return NotFound(ModelConverter.FormNewErrorResponse(DomainConstants.DisplayMessagesRoot.Display_Id_NotFound));
            }

            // Overview
            List<string> overview = new();
            try
            {
                if (!string.IsNullOrWhiteSpace(product.OverviewPoints))
                {
                    overview = JsonSerializer.Deserialize<List<string>>(product.OverviewPoints) ?? new List<string>();
                }
            }
            catch { overview = new List<string>(); }

            // Attribute dimensions
            var attrDims = await _attrDimProcess.GetByProductAsync(id);

            // Variants
            var variants = await _productVariantProcess.GetAllVariantsByProductID(id);
            var pdpVariants = new List<SpeedyMartPdpVariantSM>();
            foreach (var v in variants)
            {
                var images = await _productImagesProcess.GetProductImages(v.Id);
                var specs = await _productSpecificationProcess.GetByVariantIdAsync(v.Id);
                pdpVariants.Add(new SpeedyMartPdpVariantSM
                {
                    Variant = v,
                    Images = images,
                    Specifications = specs
                });
            }

            // Ratings & Reviews (aggregated across all variants of this product)
            var variantIds = variants.Select(v => v.Id).ToList();
            var ratings = await _apiDbContext.ProductRating
                .AsNoTracking()
                .Where(r => variantIds.Contains(r.ProductVariantId))
                .ToListAsync();

            PdpRatingSummarySM? ratingSummary = null;
            var reviews = new List<PdpReviewSM>();

            if (ratings.Any())
            {
                var avgRate = Math.Round(ratings.Average(r => (decimal)r.Rate), 1);
                var totalRatings = ratings.Count;
                var highRated = ratings.Count(r => r.Rate >= 4);
                var recommendPct = totalRatings > 0 ? (int)Math.Round(highRated * 100.0 / totalRatings) : 0;

                var tiers = Enumerable.Range(1, 5).Select(star => new PdpRatingTierSM
                {
                    Stars = star,
                    Count = ratings.Count(r => r.Rate == star)
                }).OrderByDescending(t => t.Stars).ToList();

                ratingSummary = new PdpRatingSummarySM
                {
                    Rate = avgRate,
                    TotalRatings = totalRatings,
                    RecommendPercent = recommendPct,
                    Tiers = tiers
                };

                // Get user names for reviews
                var userIds = ratings.Select(r => r.UserId).Distinct().ToList();
                var userNames = await _apiDbContext.User
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Name, u.Username })
                    .ToDictionaryAsync(u => u.Id);

                reviews = ratings
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(20)
                    .Select(r =>
                    {
                        userNames.TryGetValue(r.UserId, out var usr);
                        return new PdpReviewSM
                        {
                            UserName = usr?.Name ?? usr?.Username ?? "User",
                            Rating = r.Rate,
                            Body = r.Review,
                            CreatedAt = r.CreatedAt ?? DateTime.MinValue,
                            VerifiedPurchase = true
                        };
                    }).ToList();
            }

            // Q&A from ProductFaq
            var qaItems = await _apiDbContext.ProductFaq
                .AsNoTracking()
                .Where(f => variantIds.Contains(f.ProductVariantId ?? 0) && f.Status)
                .OrderByDescending(f => f.CreatedAt)
                .Take(20)
                .Select(f => new PdpQaItemSM
                {
                    Question = f.Question,
                    Answer = f.Answer,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue
                }).ToListAsync();

            var payload = new SpeedyMartPdpSM
            {
                Product = product,
                OverviewPoints = overview,
                AttributeDimensions = attrDims,
                Variants = pdpVariants,
                Rating = ratingSummary,
                Reviews = reviews,
                QaItems = qaItems
            };

            return ModelConverter.FormNewSuccessResponse(payload);
        }

        #endregion SPEEDYMART PDP AGGREGATION

    }
}
