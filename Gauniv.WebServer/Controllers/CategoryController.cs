using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Services;

namespace Gauniv.WebServer.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllAsync();
            ViewData["IsAdmin"] = User?.IsInRole("Admin") ?? false;
            return View(categories);
        }

        [Authorize(Policy = "Admins")]
        public IActionResult Form(int? id = null)
        {
            if (id.HasValue)
            {
                var category = _categoryService.GetByIdAsync(id.Value).Result;
                if (category == null)
                {
                    return NotFound();
                }

                ViewData["Title"] = "Edit Category";
                ViewData["Action"] = "Edit";
                return View(category);
            }

            ViewData["Title"] = "Create Category";
            ViewData["Action"] = "Create";
            return View(new Category());
        }

        [HttpPost]
        [Authorize(Policy = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryService.CreateAsync(category);
                    TempData["Message"] = $"Category '{category.Name}' was successfully created.";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("Name", ex.Message);
                }
            }

            ViewData["Title"] = "Create Category";
            ViewData["Action"] = "Create";
            return View("Form", category);
        }

        [HttpPost]
        [Authorize(Policy = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryService.UpdateAsync(category);
                    TempData["Message"] = $"Category '{category.Name}' was successfully updated.";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("Name", ex.Message);
                }
            }

            ViewData["Title"] = "Edit Category";
            ViewData["Action"] = "Edit";
            return View("Form", category);
        }

        [Authorize(Policy = "Admins")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetByIdWithGamesAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            ViewData["GamesCount"] = category.Games.Count();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _categoryService.DeleteAsync(id);
                TempData["Message"] = "Category was successfully deleted.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}