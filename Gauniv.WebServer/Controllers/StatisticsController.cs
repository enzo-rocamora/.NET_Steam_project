using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Models;
using Gauniv.WebServer.Services;

namespace Gauniv.WebServer.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly StatisticsService _statisticsService;
        private readonly ConnectionTrackingService _connectionTracking;

        public StatisticsController(
            ApplicationDbContext context,
            StatisticsService statisticsService,
            ConnectionTrackingService connectionTracking)
        {
            _context = context;
            _statisticsService = statisticsService;
            _connectionTracking = connectionTracking;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _statisticsService.GetGlobalStatisticsAsync();
            // Ajouter les statistiques en temps réel
            var (totalOnline, inGame) = _connectionTracking.GetUserStats();
            stats.CurrentOnlinePlayers = totalOnline;
            stats.CurrentInGamePlayers = inGame;
            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> GameSearch(string term)
        {
            var games = await _context.Games
                .Where(g => g.Title.Contains(term))
                .Select(g => new { id = g.Id, value = g.Title })
                .Take(10)
                .ToListAsync();

            return Json(games);
        }

        public async Task<IActionResult> GameDetails(int id)
        {
            var stats = await _statisticsService.GetGameStatisticsAsync(id);
            if (stats == null)
            {
                return NotFound();
            }
            var playersPerGame = _connectionTracking.GetPlayersPerGame();
            stats.CurrentPlayers = playersPerGame.GetValueOrDefault(id, 0);
            return View(stats);
        }

        public async Task<IActionResult> CategoryDetails(int id)
        {
            var stats = await _statisticsService.GetCategoryStatisticsAsync(id);
            if (stats == null)
            {
                return NotFound();
            }
            return View(stats);
        }
    }
}