using daleel_e_shop.BLL.DTOs.SubCategories;
using daleel_e_shop.BLL.Services.Files;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.SubCategories
{
    public class SubCategoryService : ISubCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public SubCategoryService(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<IEnumerable<SubCategoryDto>> GetAllAsync()
        {
            var subCategories = await _unitOfWork.SubCategories.FindAsync(sc => true, new[] { "Category" });
            return subCategories.Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                ImageUrl = sc.ImageUrl,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.Name : string.Empty
            });
        }

        public async Task<IEnumerable<SubCategoryDto>> GetByCategoryIdAsync(int categoryId)
        {
            var subCategories = await _unitOfWork.SubCategories.FindAsync(sc => sc.CategoryId == categoryId, new[] { "Category" });
            return subCategories.Select(sc => new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                ImageUrl = sc.ImageUrl,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.Name : string.Empty
            });
        }

        public async Task<SubCategoryDto?> GetByIdAsync(int id)
        {
            var sc = await _unitOfWork.SubCategories.FindSingleAsync(c => c.Id == id, new[] { "Category" });
            if (sc == null) return null;

            return new SubCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Description = sc.Description,
                ImageUrl = sc.ImageUrl,
                CategoryId = sc.CategoryId,
                CategoryName = sc.Category != null ? sc.Category.Name : string.Empty
            };
        }

        public async Task<SubCategoryDto?> CreateAsync(CreateSubCategoryDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
            if (category == null) return null;

            var subCategory = new SubCategory
            {
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId
            };

            if (dto.Image != null)
            {
                subCategory.ImageUrl = await _fileService.SaveFileAsync(dto.Image, "subcategories");
            }

            await _unitOfWork.SubCategories.AddAsync(subCategory);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(subCategory.Id);
        }

        public async Task<SubCategoryDto?> UpdateAsync(int id, CreateSubCategoryDto dto)
        {
            var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
            if (subCategory == null) return null;

            var category = await _unitOfWork.Categories.GetByIdAsync(dto.CategoryId);
            if (category == null) return null;

            subCategory.Name = dto.Name;
            subCategory.Description = dto.Description;
            subCategory.CategoryId = dto.CategoryId;

            if (dto.Image != null)
            {
                if (!string.IsNullOrEmpty(subCategory.ImageUrl))
                {
                    _fileService.DeleteFile(subCategory.ImageUrl);
                }
                subCategory.ImageUrl = await _fileService.SaveFileAsync(dto.Image, "subcategories");
            }

            _unitOfWork.SubCategories.Update(subCategory);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(subCategory.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var subCategory = await _unitOfWork.SubCategories.GetByIdAsync(id);
            if (subCategory == null) return false;

            if (!string.IsNullOrEmpty(subCategory.ImageUrl))
            {
                _fileService.DeleteFile(subCategory.ImageUrl);
            }

            _unitOfWork.SubCategories.Delete(subCategory);
            await _unitOfWork.CompleteAsync();

            return true;
        }
    }
}
