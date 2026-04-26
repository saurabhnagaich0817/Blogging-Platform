using InkWell.CategoryService.DTOs;
using InkWell.CategoryService.Services;
using InkWell.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InkWell.CategoryService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create a Category", Description = "Allows Admins to create new categories.")]
        [SwaggerResponse(201, "Category created successfully", typeof(CategoryResponseDTO))]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDTO dto)
        {
            var response = await _service.CreateCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategoryBySlug), new { slug = response.Slug }, new BaseResponse<CategoryResponseDTO>(true, "Category created successfully", response));
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all categories", Description = "Fetches the full list of categories.")]
        [SwaggerResponse(200, "List of categories", typeof(IEnumerable<CategoryResponseDTO>))]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _service.GetAllCategoriesAsync();
            return Ok(new BaseResponse<IEnumerable<CategoryResponseDTO>>(true, "Categories fetched successfully", categories));
        }

        [HttpGet("{slug}")]
        [SwaggerOperation(Summary = "Get category by slug", Description = "Fetches a specific category details by its unique slug.")]
        [SwaggerResponse(200, "Category found", typeof(CategoryResponseDTO))]
        [SwaggerResponse(404, "Category not found")]
        public async Task<IActionResult> GetCategoryBySlug(string slug)
        {
            var category = await _service.GetCategoryBySlugAsync(slug);
            if (category == null)
            {
                return NotFound(new BaseResponse<string>(false, "Category not found", null));
            }

            return Ok(new BaseResponse<CategoryResponseDTO>(true, "Category fetched successfully", category));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete a category", Description = "Deletes a category entirely. Admin only.")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var success = await _service.DeleteCategoryAsync(id);
            if (!success)
            {
                return NotFound(new BaseResponse<string>(false, "Category not found", null));
            }

            return Ok(new BaseResponse<string>(true, "Category deleted successfully", null));
        }
    }
}
