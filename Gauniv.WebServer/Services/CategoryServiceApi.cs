using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos.Categories;

namespace Gauniv.WebServer.Services
{
    public class CategoryApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CategoryApiService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }
    }
}