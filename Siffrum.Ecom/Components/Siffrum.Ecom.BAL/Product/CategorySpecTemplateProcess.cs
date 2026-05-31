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
    public class CategorySpecTemplateProcess : SiffrumBalBase
    {
        private readonly ILoginUserDetail _loginUserDetail;

        public CategorySpecTemplateProcess(
            IMapper mapper,
            ApiDbContext apiDbContext,
            ILoginUserDetail loginUserDetail)
            : base(mapper, apiDbContext)
        {
            _loginUserDetail = loginUserDetail;
        }

        #region GET BY CATEGORY

        public async Task<List<CategorySpecTemplateSM>> GetByCategoryAsync(long categoryId)
        {
            var exists = await _apiDbContext.Category.AnyAsync(x => x.Id == categoryId);
            if (!exists)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Category not found");

            var dms = await _apiDbContext.CategorySpecTemplates
                .AsNoTracking()
                .Where(x => x.CategoryId == categoryId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return _mapper.Map<List<CategorySpecTemplateSM>>(dms);
        }

        #endregion

        #region BULK SAVE (replace all for category)

        public async Task<List<CategorySpecTemplateSM>> BulkSaveAsync(
            long categoryId,
            List<CategorySpecTemplateSM> templates)
        {
            if (templates == null)
                throw new SiffrumException(ApiErrorTypeSM.ModelError_NoLog, "Templates list required");

            var exists = await _apiDbContext.Category.AnyAsync(x => x.Id == categoryId);
            if (!exists)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Category not found");

            // Validate
            foreach (var t in templates)
            {
                if (string.IsNullOrWhiteSpace(t.SpecKey))
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Spec key is required");
                if (string.IsNullOrWhiteSpace(t.SpecLabel))
                    throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Spec label is required");
            }

            // Delete existing, replace with new
            var existing = await _apiDbContext.CategorySpecTemplates
                .Where(x => x.CategoryId == categoryId)
                .ToListAsync();
            _apiDbContext.CategorySpecTemplates.RemoveRange(existing);

            var dmList = templates.Select((t, i) => new CategorySpecTemplateDM
            {
                CategoryId = categoryId,
                SpecKey = t.SpecKey.Trim(),
                SpecLabel = t.SpecLabel.Trim(),
                SpecGroup = t.SpecGroup?.Trim(),
                Placeholder = t.Placeholder?.Trim(),
                IsRequired = t.IsRequired,
                DisplayOrder = t.DisplayOrder > 0 ? t.DisplayOrder : i,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            }).ToList();

            await _apiDbContext.CategorySpecTemplates.AddRangeAsync(dmList);
            await _apiDbContext.SaveChangesAsync();

            return await GetByCategoryAsync(categoryId);
        }

        #endregion

        #region ADD SINGLE

        public async Task<CategorySpecTemplateSM> AddAsync(long categoryId, CategorySpecTemplateSM sm)
        {
            if (string.IsNullOrWhiteSpace(sm.SpecKey))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Spec key required");
            if (string.IsNullOrWhiteSpace(sm.SpecLabel))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Spec label required");

            var exists = await _apiDbContext.Category.AnyAsync(x => x.Id == categoryId);
            if (!exists)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Category not found");

            var dm = new CategorySpecTemplateDM
            {
                CategoryId = categoryId,
                SpecKey = sm.SpecKey.Trim(),
                SpecLabel = sm.SpecLabel.Trim(),
                SpecGroup = sm.SpecGroup?.Trim(),
                Placeholder = sm.Placeholder?.Trim(),
                IsRequired = sm.IsRequired,
                DisplayOrder = sm.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _loginUserDetail.LoginId
            };

            _apiDbContext.CategorySpecTemplates.Add(dm);
            await _apiDbContext.SaveChangesAsync();

            return _mapper.Map<CategorySpecTemplateSM>(dm);
        }

        #endregion

        #region UPDATE SINGLE

        public async Task<CategorySpecTemplateSM> UpdateAsync(long id, CategorySpecTemplateSM sm)
        {
            var dm = await _apiDbContext.CategorySpecTemplates.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Template not found");

            if (string.IsNullOrWhiteSpace(sm.SpecKey))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Spec key required");
            if (string.IsNullOrWhiteSpace(sm.SpecLabel))
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Spec label required");

            dm.SpecKey = sm.SpecKey.Trim();
            dm.SpecLabel = sm.SpecLabel.Trim();
            dm.SpecGroup = sm.SpecGroup?.Trim();
            dm.Placeholder = sm.Placeholder?.Trim();
            dm.IsRequired = sm.IsRequired;
            dm.DisplayOrder = sm.DisplayOrder;
            dm.UpdatedAt = DateTime.UtcNow;
            dm.UpdatedBy = _loginUserDetail.LoginId;

            await _apiDbContext.SaveChangesAsync();
            return _mapper.Map<CategorySpecTemplateSM>(dm);
        }

        #endregion

        #region DELETE SINGLE

        public async Task<DeleteResponseRoot> DeleteAsync(long id)
        {
            var dm = await _apiDbContext.CategorySpecTemplates.FindAsync(id);
            if (dm == null)
                throw new SiffrumException(ApiErrorTypeSM.InvalidInputData_NoLog, "Template not found");

            _apiDbContext.CategorySpecTemplates.Remove(dm);
            await _apiDbContext.SaveChangesAsync();
            return new DeleteResponseRoot(true, "Template deleted");
        }

        #endregion
    }
}
