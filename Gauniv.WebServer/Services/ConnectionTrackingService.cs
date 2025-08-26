using System.Collections.Concurrent;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Services
{
    public class ConnectionTrackingService
    {
        private ConcurrentDictionary<string, UserConnection> _connections = new();

        public void AddConnection(string connectionId, string userId, string userName)
        {
            _connections.TryAdd(connectionId, new UserConnection
            {
                ConnectionId = connectionId,
                UserId = userId,
                UserName = userName,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                CurrentStatus = UserStatus.Online
            });
        }

        public void UpdateStatus(string connectionId, UserStatus status, int? gameId = null)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                connection.CurrentStatus = status;
                connection.CurrentGameId = gameId;
                connection.LastActivity = DateTime.UtcNow;
            }
        }

        public void RemoveConnection(string connectionId)
        {
            _connections.TryRemove(connectionId, out _);
        }

        public IEnumerable<UserConnection> GetAllConnections()
        {
            return _connections.Values;
        }

        public void UpdateLastActivity(string connectionId)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                connection.LastActivity = DateTime.UtcNow;
            }
        }

        public UserConnection? GetConnectionByUserId(string userId)
        {
            return _connections.Values.FirstOrDefault(c => c.UserId == userId);
        }

        // Ajout de la méthode manquante
        public UserConnection? GetConnection(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var connection);
            return connection;
        }

        public (int totalOnline, int inGame) GetUserStats()
        {
            var connections = _connections.Values;
            return (
                totalOnline: connections.Count,
                inGame: connections.Count(c => c.CurrentStatus == UserStatus.InGame)
            );
        }

        public Dictionary<int, int> GetPlayersPerGame()
        {
            return _connections.Values
                .Where(c => c.CurrentStatus == UserStatus.InGame && c.CurrentGameId.HasValue)
                .GroupBy(c => c.CurrentGameId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()
                );
        }
    }

    public class UserConnection
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public DateTime LastActivity { get; set; }
        public UserStatus CurrentStatus { get; set; }
        public int? CurrentGameId { get; set; }
    }
}