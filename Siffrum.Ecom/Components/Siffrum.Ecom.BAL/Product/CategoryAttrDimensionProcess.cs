using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Siffrum.Ecom.BAL.Foundation.Base;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.DomainModels.v1;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using Siffrum.Ecom.ServiceModels.v1;

namespace Siffrum.Ecom.BAL.Product
{
    public class CategoryAttrDimensionProcess : SiffrumBalBase
    {
        public CategoryAttrDimensionProcess(IMapper mapper, ApiDbContext apiDbContext)
            : base(mapper, apiDbContext) { }

        public async Task<List<CategoryAttrDimensionSM>> GetByCategory(long categoryId)
        {
            var dms = await _apiDbContext.CategoryAttrDimensions
                .AsNoTracking()
                .Where(x => x.CategoryId == categoryId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();
            return _mapper.Map<List<CategoryAttrDimensionSM>>(dms);
        }

        public async Task<List<CategoryAttrDimensionSM>> BulkSave(long categoryId, List<CategoryAttrDimensionSM> items)
        {
            var existing = await _apiDbContext.CategoryAttrDimensions
                .Where(x => x.CategoryId == categoryId)
                .ToListAsync();
            _apiDbContext.CategoryAttrDimensions.RemoveRange(existing);

            var newDms = items.Select((item, i) => new CategoryAttrDimensionDM
            {
                CategoryId = categoryId,
                Name = item.Name,
                ValuesJson = item.ValuesJson,
                IsRequired = item.IsRequired,
                DisplayOrder = item.DisplayOrder == 0 ? i : item.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            await _apiDbContext.CategoryAttrDimensions.AddRangeAsync(newDms);
            await _apiDbContext.SaveChangesAsync();
            return _mapper.Map<List<CategoryAttrDimensionSM>>(newDms);
        }

        public async Task<CategoryAttrDimensionSM> Add(CategoryAttrDimensionSM request)
        {
            var dm = _mapper.Map<CategoryAttrDimensionDM>(request);
            dm.CreatedAt = DateTime.UtcNow;
            await _apiDbContext.CategoryAttrDimensions.AddAsync(dm);
            await _apiDbContext.SaveChangesAsync();
            return _mapper.Map<CategoryAttrDimensionSM>(dm);
        }

        public async Task<CategoryAttrDimensionSM> Update(CategoryAttrDimensionSM request)
        {
            var dm = await _apiDbContext.CategoryAttrDimensions.FindAsync(request.Id);
            if (dm == null) throw new Exception("Attribute dimension not found");
            dm.Name = request.Name;
            dm.ValuesJson = request.ValuesJson;
            dm.IsRequired = request.IsRequired;
            dm.DisplayOrder = request.DisplayOrder;
            dm.UpdatedAt = DateTime.UtcNow;
            await _apiDbContext.SaveChangesAsync();
            return _mapper.Map<CategoryAttrDimensionSM>(dm);
        }

        public async Task<DeleteResponseRoot> Delete(long id)
        {
            var dm = await _apiDbContext.CategoryAttrDimensions.FindAsync(id);
            if (dm == null) throw new Exception("Attribute dimension not found");
            _apiDbContext.CategoryAttrDimensions.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new DeleteResponseRoot(true, "Attribute dimension deleted");
        }
    }
}
