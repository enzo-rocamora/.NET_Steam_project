using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Models;
using System.Linq;

namespace Gauniv.WebServer.Services
{
    public class StatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GlobalStatisticsViewModel> GetGlobalStatisticsAsync()
        {
            // Charger toutes les données avec les statistiques de jeu
            var categories = await _context.Categories
                .Include(c => c.Games)
                    .ThenInclude(g => g.Owners)
                        .ThenInclude(u => u.Statistics)
                .ToListAsync();

            var allGameStats = await _context.GameStatistics.ToListAsync();
            var totalPlayTime = TimeSpan.FromTicks(allGameStats.Sum(s => s.TotalPlayTime.Ticks));

            var categoryStats = categories.Select(c =>
            {
                var allGameStats = c.Games
                    .SelectMany(g => g.Owners
                        .SelectMany(u => u.Statistics
                            .Where(s => s.GameId == g.Id)))
                    .ToList();

                return new CategoryStatistics
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    GamesCount = c.Games.Count(),
                    TotalPurchases = c.Games.Sum(g => g.Owners.Count()),
                    TotalPlayTime = TimeSpan.FromTicks(
                        allGameStats.Sum(s => s.TotalPlayTime.Ticks)
                    )
                };
            }).ToList();

            return new GlobalStatisticsViewModel
            {
                TotalGames = await _context.Games.CountAsync(),
                TotalPlayTime = totalPlayTime,
                CategoryStatistics = categoryStats
            };
        }

        public async Task<CategoryStatisticsViewModel?> GetCategoryStatisticsAsync(int categoryId)
        {
            var category = await _context.Categories
                .Include(c => c.Games)
                    .ThenInclude(g => g.Owners)
                        .ThenInclude(u => u.Statistics)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null) return null;

            var gameStats = category.Games.Select(g =>
            {
                var gameStats = g.Owners
                    .SelectMany(u => u.Statistics
                        .Where(s => s.GameId == g.Id))
                    .ToList();

                var playTime = TimeSpan.FromTicks(
                    gameStats.Sum(s => s.TotalPlayTime.Ticks)
                );

                return new GameStatistics
                {
                    GameId = g.Id,
                    GameTitle = g.Title,
                    Purchases = g.Owners.Count(),
                    PlayTime = playTime,
                    AveragePlayTime = g.Owners.Any()
                        ? TimeSpan.FromTicks(playTime.Ticks / g.Owners.Count())
                        : TimeSpan.Zero
                };
            }).ToList();

            var totalPurchases = gameStats.Sum(g => g.Purchases);
            var totalPlayTime = TimeSpan.FromTicks(gameStats.Sum(g => g.PlayTime.Ticks));

            return new CategoryStatisticsViewModel
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                GamesCount = category.Games.Count(),
                TotalPurchases = totalPurchases,
                TotalPlayTime = totalPlayTime,
                AveragePlayTimePerPurchase = totalPurchases > 0
                    ? TimeSpan.FromTicks(totalPlayTime.Ticks / totalPurchases)
                    : TimeSpan.Zero,
                GameStatistics = gameStats
            };
        }

        public async Task<GameStatisticsViewModel?> GetGameStatisticsAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Owners)
                .Include(g => g.Categories)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return null;

            // Charge séparément toutes les statistiques pour ce jeu
            var gameStats = await _context.GameStatistics
                .Where(s => s.GameId == gameId)
                .ToListAsync();

            var totalPlayTime = TimeSpan.FromTicks(gameStats.Sum(s => s.TotalPlayTime.Ticks));

            var purchaseCount = game.Owners.Count();
            var averagePlayTime = purchaseCount > 0
                ? TimeSpan.FromTicks(totalPlayTime.Ticks / purchaseCount)
                : TimeSpan.Zero;

            return new GameStatisticsViewModel
            {
                GameId = game.Id,
                GameTitle = game.Title,
                TotalPurchases = purchaseCount,
                CurrentPlayers = await GetOnlinePlayersCount(gameId), // À implémenter avec votre service en ligne
                TotalPlayTime = totalPlayTime,
                AveragePlayTime = averagePlayTime,
                Categories = game.Categories.Select(c => c.Name).ToList()
            };
        }

        private async Task<int> GetOnlinePlayersCount(int gameId)
        {
            // TODO: Implémenter avec votre service de suivi des joueurs en ligne
            return 0;
        }
    }
}