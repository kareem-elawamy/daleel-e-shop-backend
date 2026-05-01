using daleel_e_shop.BLL.DTOs.SubCategories;
using daleel_e_shop.BLL.Services.SubCategories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace daleel_e_shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubCategoriesController : ControllerBase
    {
        private readonly ISubCategoryService _subCategoryService;

        public SubCategoriesController(ISubCategoryService subCategoryService)
        {
            _subCategoryService = subCategoryService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var subCategories = await _subCategoryService.GetAllAsync();
            return Ok(subCategories);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategoryId(int categoryId)
        {
            var subCategories = await _subCategoryService.GetByCategoryIdAsync(categoryId);
            return Ok(subCategories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var subCategory = await _subCategoryService.GetByIdAsync(id);
            if (subCategory == null) return NotFound($"SubCategory with ID {id} not found.");
            return Ok(subCategory);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] CreateSubCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var subCategory = await _subCategoryService.CreateAsync(dto);
            if (subCategory == null) return BadRequest("Invalid Category ID provided.");

            return CreatedAtAction(nameof(GetById), new { id = subCategory.Id }, subCategory);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateSubCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var subCategory = await _subCategoryService.UpdateAsync(id, dto);
            if (subCategory == null) return NotFound($"SubCategory with ID {id} not found or invalid Category ID.");

            return Ok(subCategory);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _subCategoryService.DeleteAsync(id);
            if (!result) return NotFound($"SubCategory with ID {id} not found.");

            return NoContent();
        }
    }
}
