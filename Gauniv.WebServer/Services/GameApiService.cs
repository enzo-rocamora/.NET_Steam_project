using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using AutoMapper;
using Gauniv.WebServer.Dtos.Common;
using Gauniv.WebServer.Dtos.Games;

namespace Gauniv.WebServer.Services
{
    public class GameApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public GameApiService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<PaginatedResponse<GameDto>> GetGamesAsync(GameQueryParameters parameters)
        {
            var query = _context.Games
                .Include(g => g.Categories)
                .AsNoTracking();

            // Appliquer les filtres de catégories
            if (parameters.Categories != null && parameters.Categories.Any())
            {
                query = query.Where(g => g.Categories.Any(c => parameters.Categories.Contains(c.Id)));
            }

            // Compter le total avant la pagination
            var totalCount = await query.CountAsync();

            // Appliquer la pagination
            var pageSize = parameters.Limit ?? 10;
            var offset = parameters.Offset ?? 0;
            var currentPage = (offset / pageSize) + 1;

            var games = await query
                .Skip(offset)
                .Take(pageSize)
                .ToListAsync();

            // Calculer les informations de pagination
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var response = new PaginatedResponse<GameDto>
            {
                Items = _mapper.Map<IEnumerable<GameDto>>(games),
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                HasNext = currentPage < totalPages,
                HasPrevious = currentPage > 1
            };

            return response;
        }
    }
}