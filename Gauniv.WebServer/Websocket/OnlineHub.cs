using Gauniv.WebServer.Data;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Gauniv.WebServer.Websocket
{
    public class OnlineHub : Hub
    {
        private readonly UserManager<User> _userManager;
        private readonly ConnectionTrackingService _connectionTracking;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OnlineHub> _logger;

        public OnlineHub(
            UserManager<User> userManager,
            ConnectionTrackingService connectionTracking,
            ApplicationDbContext context,
            ILogger<OnlineHub> logger)
        {
            _userManager = userManager;
            _connectionTracking = connectionTracking;
            _context = context;
            _logger = logger;
        }

        private async Task BroadcastPlayerCounts()
        {
            var (totalOnline, inGame) = _connectionTracking.GetUserStats();
            var playersPerGame = _connectionTracking.GetPlayersPerGame();

            await Clients.All.SendAsync("PlayerCountsUpdated", new
            {
                totalOnline,
                inGame,
                perGame = playersPerGame
            });
        }

        public async override Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Connection attempt without user ID");
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return;
            }

            // Ajouter la connexion
            _connectionTracking.AddConnection(Context.ConnectionId, userId, user.UserName);

            // Notifier les autres utilisateurs
            await Clients.All.SendAsync("UserStatusChanged", new
            {
                userId = user.Id,
                userName = user.UserName,
                status = UserStatus.Online,
                timestamp = DateTime.UtcNow
            });

            // Diffuser les nouveaux compteurs
            await BroadcastPlayerCounts();

            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception? exception)
        {
            var connection = _connectionTracking.GetConnection(Context.ConnectionId);
            if (connection != null)
            {
                // Si l'utilisateur était en jeu, sauvegarder les statistiques
                if (connection.CurrentStatus == UserStatus.InGame && connection.CurrentGameId.HasValue)
                {
                    await SaveGameStatistics(connection.UserId, connection.CurrentGameId.Value, connection.LastActivity);
                }

                _connectionTracking.RemoveConnection(Context.ConnectionId);

                // Notifier les autres utilisateurs
                await Clients.All.SendAsync("UserStatusChanged", new
                {
                    userId = connection.UserId,
                    userName = connection.UserName,
                    status = UserStatus.Offline,
                    timestamp = DateTime.UtcNow
                });

                // Diffuser les nouveaux compteurs
                await BroadcastPlayerCounts();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task UpdateGameStatus(int? gameId)
        {
            var connection = _connectionTracking.GetConnection(Context.ConnectionId);
            if (connection == null) return;

            if (connection.CurrentStatus == UserStatus.InGame &&
                connection.CurrentGameId.HasValue &&
                (!gameId.HasValue || gameId.Value != connection.CurrentGameId.Value))
            {
                await SaveGameStatistics(connection.UserId, connection.CurrentGameId.Value, connection.LastActivity);
            }

            // Vérifier que le jeu existe et obtenir son nom
            string? gameName = null;
            if (gameId.HasValue)
            {
                var game = await _context.Games
                    .Where(g => g.Id == gameId.Value)
                    .Select(g => new { g.Id, g.Title })
                    .FirstOrDefaultAsync();

                if (game == null)
                {
                    _logger.LogWarning("Attempt to start non-existent game: {GameId}", gameId.Value);
                    return;
                }

                gameName = game.Title;
            }

            var newStatus = gameId.HasValue ? UserStatus.InGame : UserStatus.Online;
            _connectionTracking.UpdateStatus(Context.ConnectionId, newStatus, gameId);

            await Clients.All.SendAsync("UserStatusChanged", new
            {
                userId = connection.UserId,
                userName = connection.UserName,
                status = newStatus,
                gameId = gameId,
                gameName = gameName,
                timestamp = DateTime.UtcNow
            });

            // Diffuser les nouveaux compteurs après le changement de statut
            await BroadcastPlayerCounts();
        }

        private async Task SaveGameStatistics(string userId, int gameId, DateTime startTime)
        {
            try
            {
                var playTime = DateTime.UtcNow - startTime;

                var stats = await _context.GameStatistics
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.GameId == gameId);

                if (stats == null)
                {
                    stats = new GameStatistic
                    {
                        UserId = userId,
                        GameId = gameId,
                        TotalPlayTime = playTime,
                        TimesPlayed = 1,
                        LastPlayedAt = DateTime.UtcNow
                    };
                    _context.GameStatistics.Add(stats);
                }
                else
                {
                    stats.TotalPlayTime += playTime;
                    stats.TimesPlayed++;
                    stats.LastPlayedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving game statistics for user {UserId} in game {GameId}",
                    userId, gameId);
            }
        }
    }
}