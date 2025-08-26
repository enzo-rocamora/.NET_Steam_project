using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Services
{
    public class GameService
    {
        private readonly ApplicationDbContext _context;

        public GameService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Game>> GetAllAsync(string sortBy = "name", int[]? categories = null)
        {
            var query = _context.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners) // Include the Owners relationship
                .AsQueryable();

            // Apply category filter
            if (categories != null && categories.Any())
            {
                query = query.Where(g => g.Categories.Any(c => categories.Contains(c.Id)));
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "price" => query.OrderBy(g => g.Price),
                "price_desc" => query.OrderByDescending(g => g.Price),
                "date" => query.OrderByDescending(g => g.CreatedAt),
                "name_desc" => query.OrderByDescending(g => g.Title),
                _ => query.OrderBy(g => g.Title)
            };

            return await query.ToListAsync();
        }

        public async Task<Game?> GetByIdAsync(int id)
        {
            return await _context.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners) // Include the Owners relationship
                .FirstOrDefaultAsync(g => g.Id == id);
        }
        public async Task<Game?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task CreateAsync(Game game, List<int> selectedCategories)
        {
            if (await _context.Games.AnyAsync(g => g.Title == game.Title))
            {
                throw new InvalidOperationException("A game with this title already exists.");
            }

            if (game.Price < 0)
            {
                throw new InvalidOperationException("Price cannot be negative.");
            }

            if (selectedCategories != null && selectedCategories.Any())
            {
                var categories = await _context.Categories
                    .Where(c => selectedCategories.Contains(c.Id))
                    .ToListAsync();

                if (categories.Count != selectedCategories.Count)
                {
                    throw new InvalidOperationException("One or more selected categories do not exist.");
                }

                game.Categories = categories;
            }

            game.CreatedAt = DateTime.UtcNow;
            game.UpdatedAt = DateTime.UtcNow;

            await _context.Games.AddAsync(game);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Game game, List<int> selectedCategories)
        {
            var existingGame = await _context.Games
                .Include(g => g.Categories)
                .FirstOrDefaultAsync(g => g.Id == game.Id);

            if (existingGame == null)
            {
                throw new InvalidOperationException("Game not found.");
            }

            var duplicateTitle = await _context.Games
                .Where(g => g.Title == game.Title && g.Id != game.Id)
                .AnyAsync();

            if (duplicateTitle)
            {
                throw new InvalidOperationException("A game with this title already exists.");
            }

            if (game.Price < 0)
            {
                throw new InvalidOperationException("Price cannot be negative.");
            }

            // Update basic properties
            existingGame.Title = game.Title;
            existingGame.Description = game.Description;
            existingGame.Price = game.Price;
            existingGame.PayloadPath = game.PayloadPath;
            existingGame.UpdatedAt = DateTime.UtcNow;
            existingGame.FileSize = game.FileSize;

            // Update categories
            var categoryList = existingGame.Categories.ToList();
            categoryList.Clear();

            if (selectedCategories != null && selectedCategories.Any())
            {
                var categories = await _context.Categories
                    .Where(c => selectedCategories.Contains(c.Id))
                    .ToListAsync();

                if (categories.Count != selectedCategories.Count)
                {
                    throw new InvalidOperationException("One or more selected categories do not exist.");
                }

                foreach (var category in categories)
                {
                    categoryList.Add(category);
                }
            }

            existingGame.Categories = categoryList;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var game = await GetByIdWithDetailsAsync(id);
            if (game == null)
                throw new InvalidOperationException("Game not found.");

            if (game.Owners.Any())
            {
                throw new InvalidOperationException($"Cannot delete game '{game.Title}' because it has been purchased by users.");
            }

            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}