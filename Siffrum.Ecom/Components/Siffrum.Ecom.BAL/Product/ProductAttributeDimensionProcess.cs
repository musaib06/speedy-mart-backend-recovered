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

namespace Siffrum.Ecom.BAL.Product
{
    public class ProductAttributeDimensionProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public ProductAttributeDimensionProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        public async Task<List<ProductAttributeDimensionSM>> GetByProductAsync(long productId)
        {
            var dms = await _apiDbContext.ProductAttributeDimensions
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return _mapper.Map<List<ProductAttributeDimensionSM>>(dms);
        }

        public async Task<List<ProductAttributeDimensionSM>> BulkSaveAsync(
            long productId,
            List<ProductAttributeDimensionSM> dimensions)
        {
            if (dimensions == null)
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Dimensions list required");

            var productExists = await _apiDbContext.Product.AnyAsync(x => x.Id == productId);
            if (!productExists)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Product not found");

            foreach (var d in dimensions)
            {
                if (string.IsNullOrWhiteSpace(d.DimensionKey))
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Dimension key is required");
                if (string.IsNullOrWhiteSpace(d.DimensionLabel))
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Dimension label is required");
            }

            var existing = await _apiDbContext.ProductAttributeDimensions
                .Where(x => x.ProductId == productId)
                .ToListAsync();
            _apiDbContext.ProductAttributeDimensions.RemoveRange(existing);

            var dmList = dimensions.Select((d, i) => new ProductAttributeDimensionDM
            {
                ProductId = productId,
                DimensionKey = d.DimensionKey.Trim(),
                DimensionLabel = d.DimensionLabel.Trim(),
                DisplayType = d.DisplayType ?? "button",
                DisplayOrder = d.DisplayOrder > 0 ? d.DisplayOrder : i,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            }).ToList();

            await _apiDbContext.ProductAttributeDimensions.AddRangeAsync(dmList);
            await _apiDbContext.SaveChangesAsync();

            return await GetByProductAsync(productId);
        }

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var dm = await _apiDbContext.ProductAttributeDimensions.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Dimension not found");

            _apiDbContext.ProductAttributeDimensions.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new DeleteResponseRoot(true, "Dimension deleted");
        }
    }
}
