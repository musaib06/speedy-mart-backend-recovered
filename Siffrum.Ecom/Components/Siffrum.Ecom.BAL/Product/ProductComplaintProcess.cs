using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.ExceptionHandler;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Enums;
using Siffrum.Ecom.ServiceModels.Foundation.Base.Interfaces;
using Siffrum.Ecom.ServiceModels.v1;
using System.Text.Json;

namespace Siffrum.Ecom.BAL.Product
{
    public class ProductComplaintProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public ProductComplaintProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        #region GET ALL (Admin)

        public async Task<List<ProductComplaintSM>> GetAllAsync(
            int? status = null,
            int? complaintType = null,
            int skip = 0, int top = 50)
        {
            var query = _apiDbContext.ProductComplaints
                .AsNoTracking()
                .Include(x => x.Product)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (complaintType.HasValue)
                query = query.Where(x => x.ComplaintType == complaintType.Value);

            var dms = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            return dms.Select(MapToSM).ToList();
        }

        #endregion

        #region GET BY SELLER

        public async Task<List<ProductComplaintSM>> GetBySellerAsync(
            long sellerId, int? status = null,
            int skip = 0, int top = 50)
        {
            var query = _apiDbContext.ProductComplaints
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x => x.SellerId == sellerId);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            var dms = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip).Take(top)
                .ToListAsync();

            return dms.Select(MapToSM).ToList();
        }

        #endregion

        #region GET BY ID

        public async Task<ProductComplaintSM> GetByIdAsync(long id)
        {
            var dm = await _apiDbContext.ProductComplaints
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            var sm = MapToSM(dm);
            sm.Comments = dm.Comments?.OrderBy(c => c.CreatedAt).Select(c => new ProductComplaintCommentSM
            {
                Id = c.Id,
                ComplaintId = c.ComplaintId,
                CommenterType = c.CommenterType,
                CommenterId = c.CommenterId,
                Comment = c.Comment,
                Attachments = string.IsNullOrEmpty(c.Attachments) ? null : JsonSerializer.Deserialize<List<string>>(c.Attachments),
                CreatedAt = c.CreatedAt
            }).ToList();

            return sm;
        }

        #endregion

        #region CREATE (Seller)

        public async Task<ProductComplaintSM> CreateAsync(ProductComplaintSM sm)
        {
            if (string.IsNullOrWhiteSpace(sm.Subject))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Subject is required");
            if (string.IsNullOrWhiteSpace(sm.Description))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Description is required");

            var product = await _apiDbContext.Product.FindAsync(sm.ProductId);
            if (product == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Product not found");

            var dm = new ProductComplaintDM
            {
                ProductId = sm.ProductId,
                SellerId = sm.SellerId,
                ComplaintType = sm.ComplaintType,
                Subject = sm.Subject.Trim(),
                Description = sm.Description.Trim(),
                Attachments = sm.Attachments != null ? JsonSerializer.Serialize(sm.Attachments) : null,
                Status = 1, // Open
                Priority = sm.Priority > 0 ? sm.Priority : 2,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            };

            _apiDbContext.ProductComplaints.Add(dm);
            await _apiDbContext.SaveChangesAsync();

            return MapToSM(dm);
        }

        #endregion

        #region UPDATE STATUS (Admin)

        public async Task<BoolResponseRoot> UpdateStatusAsync(long id, int newStatus, string? resolutionNotes = null)
        {
            var dm = await _apiDbContext.ProductComplaints.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            dm.Status = newStatus;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            if (newStatus == 3) // Resolved
            {
                dm.ResolvedAt = DateTime.UtcNow;
                dm.ResolvedByAdminId = long.TryParse(_loginUserDetail.LoginId, out var adminId) ? adminId : null;
                dm.ResolutionNotes = resolutionNotes;
            }

            await _apiDbContext.SaveChangesAsync();

            var statusLabel = newStatus switch { 1 => "Open", 2 => "In Progress", 3 => "Resolved", 4 => "Closed", _ => "Updated" };
            return new BoolResponseRoot(true, $"Complaint status changed to {statusLabel}");
        }

        #endregion

        #region ASSIGN TO ADMIN

        public async Task<BoolResponseRoot> AssignAsync(long id, long adminId)
        {
            var dm = await _apiDbContext.ProductComplaints.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            dm.AssignedToAdminId = adminId;
            if (dm.Status == 1) dm.Status = 2; // Move to InProgress
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;
            await _apiDbContext.SaveChangesAsync();

            return new BoolResponseRoot(true, "Complaint assigned");
        }

        #endregion

        #region ADD COMMENT

        public async Task<ProductComplaintCommentSM> AddCommentAsync(long complaintId, ProductComplaintCommentSM sm)
        {
            var complaint = await _apiDbContext.ProductComplaints.FindAsync(complaintId);
            if (complaint == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            if (string.IsNullOrWhiteSpace(sm.Comment))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Comment is required");

            var dm = new ProductComplaintCommentDM
            {
                ComplaintId = complaintId,
                CommenterType = sm.CommenterType,
                CommenterId = sm.CommenterId,
                Comment = sm.Comment.Trim(),
                Attachments = sm.Attachments != null ? JsonSerializer.Serialize(sm.Attachments) : null,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            };

            _apiDbContext.ProductComplaintComments.Add(dm);
            await _apiDbContext.SaveChangesAsync();

            return new ProductComplaintCommentSM
            {
                Id = dm.Id,
                ComplaintId = dm.ComplaintId,
                CommenterType = dm.CommenterType,
                CommenterId = dm.CommenterId,
                Comment = dm.Comment,
                CreatedAt = dm.CreatedAt
            };
        }

        #endregion

        #region DELETE

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var dm = await _apiDbContext.ProductComplaints
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Complaint not found");

            _apiDbContext.ProductComplaintComments.RemoveRange(dm.Comments);
            _apiDbContext.ProductComplaints.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new DeleteResponseRoot(true, "Complaint deleted");
        }

        #endregion

        #region HELPERS

        private ProductComplaintSM MapToSM(ProductComplaintDM dm)
        {
            return new ProductComplaintSM
            {
                Id = dm.Id,
                ProductId = dm.ProductId,
                ProductName = dm.Product?.Name,
                SellerId = dm.SellerId,
                ComplaintType = dm.ComplaintType,
                Subject = dm.Subject,
                Description = dm.Description,
                Attachments = string.IsNullOrEmpty(dm.Attachments) ? null : JsonSerializer.Deserialize<List<string>>(dm.Attachments),
                Status = dm.Status,
                Priority = dm.Priority,
                AssignedToAdminId = dm.AssignedToAdminId,
                ResolutionNotes = dm.ResolutionNotes,
                ResolvedByAdminId = dm.ResolvedByAdminId,
                ResolvedAt = dm.ResolvedAt,
                CreatedAt = dm.CreatedAt
            };
        }

        #endregion
    }
}
