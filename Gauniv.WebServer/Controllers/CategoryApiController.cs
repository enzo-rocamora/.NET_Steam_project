using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gauniv.WebServer.Services;
using Gauniv.WebServer.Dtos.Categories;

namespace Gauniv.WebServer.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [AllowAnonymous]
    public class CategoryApiController : ControllerBase
    {
        private readonly CategoryApiService _categoryService;

        public CategoryApiController(CategoryApiService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }
    }
}