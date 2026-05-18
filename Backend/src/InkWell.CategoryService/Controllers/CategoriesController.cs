using InkWell.CategoryService.DTOs;
using InkWell.CategoryService.Services;
using InkWell.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InkWell.CategoryService.Controllers
{
    /// <summary>
    /// Controller for managing content taxonomies and categories.
    /// </summary>
    [ApiController]
    [Route("api/categories")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Retrieves all active categories available for posts.
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Fetch all categories", Description = "Returns a list of all taxonomy categories.")]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(new BaseResponse<IEnumerable<CategoryResponseDTO>>(true, "Categories fetched successfully", categories));
        }

        /// <summary>
        /// Retrieves detailed information for a specific category.
        /// </summary>
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get category by ID", Description = "Returns metadata for a specific category.")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound(new BaseResponse<string>(false, "Category not found", null));
            return Ok(new BaseResponse<CategoryResponseDTO>(true, "Category fetched successfully", category));
        }

        /// <summary>
        /// Creates a new category. Restricted to Administrators.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Create a new category", Description = "Admin only. Adds a new category to the platform taxonomy.")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDTO request)
        {
            var response = await _categoryService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.CategoryId }, new BaseResponse<CategoryResponseDTO>(true, "Category created successfully", response));
        }

        /// <summary>
        /// Updates an existing category's metadata. Restricted to Administrators.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Update a category", Description = "Admin only. Modifies existing category details.")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateCategoryDTO request)
        {
            var response = await _categoryService.UpdateCategoryAsync(id, request);
            if (response == null) return NotFound(new BaseResponse<string>(false, "Category not found", null));
            return Ok(new BaseResponse<CategoryResponseDTO>(true, "Category updated successfully", response));
        }

        /// <summary>
        /// Permanently removes a category. Restricted to Administrators.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [SwaggerOperation(Summary = "Delete a category", Description = "Admin only. Removes a category from the platform.")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _categoryService.DeleteCategoryAsync(id);
            if (!success) return NotFound(new BaseResponse<string>(false, "Category not found", null));
            return Ok(new BaseResponse<string>(true, "Category deleted successfully", null));
        }
    }
}
