using daleel_e_shop.BLL.DTOs.Products;
using daleel_e_shop.BLL.Services.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace daleel_e_shop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var products = await _productService.GetAllAsync(page, pageSize);
            return Ok(products);
        }

        [HttpGet("deals")]
        public async Task<IActionResult> GetDeals([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var deals = await _productService.GetDealsAsync(page, pageSize);
            return Ok(deals);
        }

        [HttpGet("best-sellers")]
        public async Task<IActionResult> GetBestSellers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var bestSellers = await _productService.GetBestSellersAsync(page, pageSize);
            return Ok(bestSellers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound($"Product with ID {id} not found.");
            return Ok(product);
        }

        [HttpGet("by-subcategory/{subCategoryId}")]
        public async Task<IActionResult> GetBySubCategory(int subCategoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var products = await _productService.GetBySubCategoryAsync(subCategoryId, page, pageSize);
            return Ok(products);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] ProductSearchQuery query)
        {
            var result = await _productService.SearchAsync(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = await _productService.UpdateAsync(id, dto);
            if (product == null) return NotFound($"Product with ID {id} not found.");

            return Ok(product);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productService.DeleteAsync(id);
            if (!result) return NotFound($"Product with ID {id} not found.");

            return NoContent();
        }

        // Product Images
        [HttpGet("{productId}/images")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            var images = await _productService.GetProductImagesAsync(productId);
            return Ok(images);
        }

        [HttpPost("{productId}/images")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProductImage(int productId, IFormFile image, [FromQuery] bool isPrimary = false)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided.");

            var result = await _productService.AddProductImageAsync(productId, image, isPrimary);
            if (result == null) return NotFound($"Product with ID {productId} not found.");

            return Ok(result);
        }

        [HttpDelete("images/{imageId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            var result = await _productService.DeleteProductImageAsync(imageId);
            if (!result) return NotFound("Image not found.");

            return NoContent();
        }

        [HttpPut("{productId}/images/{imageId}/set-primary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetPrimaryImage(int productId, int imageId)
        {
            var result = await _productService.SetPrimaryImageAsync(productId, imageId);
            if (!result) return NotFound("Image not found.");

            return Ok(new { Message = "Primary image set successfully." });
        }
    }
}
