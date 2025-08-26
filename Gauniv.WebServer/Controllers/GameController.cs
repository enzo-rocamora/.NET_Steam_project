using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gauniv.WebServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Gauniv.WebServer.Controllers
{
    public class GameController : Controller
    {
        private readonly GameService _gameService;
        private readonly GameFileService _gameFileService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<GameController> _logger;

        public GameController(
            GameService gameService,
            GameFileService gameFileService,
            UserManager<User> userManager,
            ILogger<GameController> logger)
        {
            _gameService = gameService;
            _gameFileService = gameFileService;
            _userManager = userManager;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string sortBy = "name", int[]? categories = null, bool showOwnedOnly = false)
        {
            // Get filters for the view
            ViewData["CurrentSort"] = sortBy;
            ViewData["NameSortParam"] = sortBy == "name" ? "name_desc" : "name";
            ViewData["PriceSortParam"] = sortBy == "price" ? "price_desc" : "price";
            ViewData["DateSortParam"] = "date";
            ViewData["Categories"] = await _gameService.GetAllCategoriesAsync();
            ViewData["SelectedCategories"] = categories ?? Array.Empty<int>();
            ViewData["ShowOwnedOnly"] = showOwnedOnly;

            // Get user information if authenticated
            List<int> ownedGameIds = new();
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                ownedGameIds = user?.OwnedGames?.Select(g => g.Id).ToList() ?? new List<int>();
                ViewData["OwnedGames"] = ownedGameIds;
                ViewData["IsAdmin"] = User.IsInRole("Admin");
            }

            var games = await _gameService.GetAllAsync(sortBy, categories);

            // Filter by owned games if requested
            if (showOwnedOnly && User?.Identity?.IsAuthenticated == true)
            {
                games = games.Where(g => ownedGameIds.Contains(g.Id)).ToList();
            }

            return View(games);
        }

        [Authorize(Policy = "Admins")]
        public async Task<IActionResult> Form(int? id = null)
        {
            ViewData["Categories"] = await _gameService.GetAllCategoriesAsync();

            if (id.HasValue)
            {
                var game = await _gameService.GetByIdAsync(id.Value);
                if (game == null)
                {
                    return NotFound();
                }

                var viewModel = new GameCreateViewModel
                {
                    Id = game.Id,
                    Title = game.Title,
                    Description = game.Description,
                    Price = game.Price,
                    ExistingPayloadPath = game.PayloadPath,
                    ExistingFileSize = game.FileSize,
                    SelectedCategories = game.Categories.Select(c => c.Id).ToList()
                };

                ViewData["Title"] = "Edit Game";
                ViewData["Action"] = "Edit";
                ViewData["SelectedCategories"] = viewModel.SelectedCategories;
                return View(viewModel);
            }

            // Pour la création
            var newViewModel = new GameCreateViewModel();
            ViewData["Title"] = "Create Game";
            ViewData["Action"] = "Create";
            ViewData["SelectedCategories"] = new List<int>();
            return View(newViewModel);
        }

        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]
        [RequestSizeLimit(int.MaxValue)]
        [HttpPost]
        [Authorize(Policy = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GameCreateViewModel viewModel)
        {
            try
            {
                if (viewModel.GameFile == null || viewModel.GameFile.Length == 0)
                {
                    ModelState.AddModelError("GameFile", "Please select a game file to upload");
                }
                else
                {
                    // Validate file extension
                    var allowedExtensions = new[] { ".exe", ".zip", ".rar", ".7z" };
                    var extension = Path.GetExtension(viewModel.GameFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("GameFile", "Invalid file type. Only .exe, .zip, .rar, and .7z files are allowed.");
                    }
                }

                if (ModelState.IsValid)
                {
                    // Save the file
                    var (fileName, fileSize) = await _gameFileService.SaveGameFileAsync(viewModel.GameFile!);

                    // Create game entity from view model
                    var game = new Game
                    {
                        Title = viewModel.Title,
                        Description = viewModel.Description,
                        Price = viewModel.Price,
                        PayloadPath = fileName,
                        FileSize = fileSize,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Create the game
                    await _gameService.CreateAsync(game, viewModel.SelectedCategories);

                    TempData["Message"] = $"Game '{game.Title}' was successfully created.";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating game: {ex.Message}");
            }

            // If we get here, something went wrong
            ViewData["Title"] = "Create Game";
            ViewData["Action"] = "Create";
            ViewData["Categories"] = await _gameService.GetAllCategoriesAsync();
            ViewData["SelectedCategories"] = viewModel.SelectedCategories;
            return View("Form", viewModel);
        }

        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue)]
        [RequestSizeLimit(int.MaxValue)]
        [HttpPost]
        [Authorize(Policy = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(GameCreateViewModel viewModel)
        {
            if (!viewModel.Id.HasValue)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingGame = await _gameService.GetByIdAsync(viewModel.Id.Value);
                    if (existingGame == null)
                        return NotFound();

                    // Update basic properties
                    existingGame.Title = viewModel.Title;
                    existingGame.Description = viewModel.Description;
                    existingGame.Price = viewModel.Price;
                    existingGame.UpdatedAt = DateTime.UtcNow;

                    // Handle file update if provided
                    if (viewModel.GameFile != null && viewModel.GameFile.Length > 0)
                    {
                        // Validate file extension
                        var allowedExtensions = new[] { ".exe", ".zip", ".rar", ".7z" };
                        var extension = Path.GetExtension(viewModel.GameFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("GameFile", "Invalid file type. Only .exe, .zip, .rar, and .7z files are allowed.");
                            ViewData["Categories"] = await _gameService.GetAllCategoriesAsync();
                            ViewData["SelectedCategories"] = viewModel.SelectedCategories;
                            ViewData["Title"] = "Edit Game";
                            ViewData["Action"] = "Edit";
                            return View("Form", viewModel);
                        }

                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(existingGame.PayloadPath))
                        {
                            _gameFileService.DeleteGameFile(existingGame.PayloadPath);
                        }

                        // Save new file
                        var (fileName, fileSize) = await _gameFileService.SaveGameFileAsync(viewModel.GameFile);
                        existingGame.PayloadPath = fileName;
                        existingGame.FileSize = fileSize;
                    }

                    // Update the game
                    await _gameService.UpdateAsync(existingGame, viewModel.SelectedCategories);

                    TempData["Message"] = $"Game '{existingGame.Title}' was successfully updated.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating game: {ex.Message}");
                }
            }

            ViewData["Title"] = "Edit Game";
            ViewData["Action"] = "Edit";
            ViewData["Categories"] = await _gameService.GetAllCategoriesAsync();
            ViewData["SelectedCategories"] = viewModel.SelectedCategories;
            return View("Form", viewModel);
        }

        [Authorize(Policy = "Admins")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var game = await _gameService.GetByIdWithDetailsAsync(id.Value);
            if (game == null)
                return NotFound();

            ViewData["OwnersCount"] = game.Owners.Count();
            return View(game);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Policy = "Admins")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var game = await _gameService.GetByIdAsync(id);
                if (game != null && !string.IsNullOrEmpty(game.PayloadPath))
                {
                    _gameFileService.DeleteGameFile(game.PayloadPath);
                }
                await _gameService.DeleteAsync(id);
                TempData["Message"] = "Game was successfully deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purchase(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                var game = await _gameService.GetByIdAsync(id);
                if (game == null)
                {
                    return NotFound();
                }

                // Check if user already owns the game
                if (game.Owners.Any(u => u.Id == userId))
                {
                    TempData["Error"] = "You already own this game";
                    return RedirectToAction(nameof(Index));
                }

                // Add the game to user's owned games
                user.OwnedGames = user.OwnedGames.Append(game).ToList();
                await _userManager.UpdateAsync(user);
                TempData["Message"] = $"Successfully purchased {game.Title}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}