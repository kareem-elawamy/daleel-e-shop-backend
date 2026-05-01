using daleel_e_shop.BLL.DTOs.Categories;
using daleel_e_shop.BLL.Services.Files;
using daleel_e_shop.DAL.Models;
using daleel_e_shop.DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileService _fileService;

        public CategoryService(IUnitOfWork unitOfWork, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            return categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl
            });
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return null;

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };
        }

        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description
            };

            if (dto.Image != null)
            {
                category.ImageUrl = await _fileService.SaveFileAsync(dto.Image, "categories");
            }

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.CompleteAsync();

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };
        }

        public async Task<CategoryDto?> UpdateAsync(int id, CreateCategoryDto dto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return null;

            category.Name = dto.Name;
            category.Description = dto.Description;

            if (dto.Image != null)
            {
                if (!string.IsNullOrEmpty(category.ImageUrl))
                {
                    _fileService.DeleteFile(category.ImageUrl);
                }
                category.ImageUrl = await _fileService.SaveFileAsync(dto.Image, "categories");
            }

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.CompleteAsync();

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null) return false;

            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                _fileService.DeleteFile(category.ImageUrl);
            }

            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.CompleteAsync();

            return true;
        }
    }
}
