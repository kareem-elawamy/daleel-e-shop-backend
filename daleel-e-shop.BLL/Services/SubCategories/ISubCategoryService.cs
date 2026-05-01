using daleel_e_shop.BLL.DTOs.SubCategories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace daleel_e_shop.BLL.Services.SubCategories
{
    public interface ISubCategoryService
    {
        Task<IEnumerable<SubCategoryDto>> GetAllAsync();
        Task<IEnumerable<SubCategoryDto>> GetByCategoryIdAsync(int categoryId);
        Task<SubCategoryDto?> GetByIdAsync(int id);
        Task<SubCategoryDto?> CreateAsync(CreateSubCategoryDto dto);
        Task<SubCategoryDto?> UpdateAsync(int id, CreateSubCategoryDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
