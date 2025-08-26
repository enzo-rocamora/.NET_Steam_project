using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Services
{
    public class CategoryService
    {
        private readonly ApplicationDbContext _context;

        public CategoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllAsync()
        {
            return await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category?> GetByIdWithGamesAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Games)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task CreateAsync(Category category)
        {
            if (await _context.Categories.AnyAsync(c => c.Name == category.Name))
            {
                throw new InvalidOperationException("A category with this name already exists.");
            }

            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            var existingCategory = await _context.Categories
                .Where(c => c.Name == category.Name)
                .FirstOrDefaultAsync(c => c.Id != category.Id);

            if (existingCategory != null)
            {
                throw new InvalidOperationException("A category with this name already exists.");
            }

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await GetByIdWithGamesAsync(id);
            if (category == null)
                throw new InvalidOperationException("Category not found.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}