using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Services;
using Gauniv.WebServer.Models;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Controllers
{
    public class StatusController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ConnectionTrackingService _connectionTracking;
        private readonly ApplicationDbContext _context;

        public StatusController(
            UserManager<User> userManager,
            ConnectionTrackingService connectionTracking,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _connectionTracking = connectionTracking;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Select(u => new OnlineUserViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            // Ajouter les statuts en temps réel
            var connections = _connectionTracking.GetAllConnections();
            foreach (var user in users)
            {
                var connection = connections.FirstOrDefault(c => c.UserId == user.UserId);
                if (connection != null)
                {
                    user.Status = connection.CurrentStatus;
                    user.CurrentGameId = connection.CurrentGameId;
                    var game = await _context.Games.FindAsync(connection.CurrentGameId);
                    if (game == null)
                    {
                        user.gameName = "Error";
                    }
                    else
                    {
                    user.gameName = game.Title;
                    }
                    user.LastActivity = connection.LastActivity;
                }
                else
                {
                    user.Status = UserStatus.Offline;
                    user.LastActivity = user.LastLoginAt; // Utiliser la dernière connexion comme dernière activité
                }
            }

            return View(users);
        }
    }
}