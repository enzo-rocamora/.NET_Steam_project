using Gauniv.WebServer.Data;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Gauniv.WebServer.Websocket { 
public class StatusHub : Hub
{
    private readonly UserApiService _userService;
    private readonly ConnectionTrackingService _connectionTracking;

    public StatusHub(UserApiService userService, ConnectionTrackingService connectionTracking)
    {
        _userService = userService;
        _connectionTracking = connectionTracking;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            userId = "1";
        }
        if (string.IsNullOrEmpty(userName))
        {
            userName = "1"; 
        }
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(userName))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _connectionTracking.AddConnection(Context.ConnectionId, userId, userName);
            await _userService.UpdateUserGameStatusAsync(userId, null);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _connectionTracking.RemoveConnection(Context.ConnectionId);
            await _userService.UpdateUserStatusOfflineAsync(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task StartGame(int gameId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            _connectionTracking.UpdateStatus(Context.ConnectionId, UserStatus.InGame, gameId);
            await _userService.UpdateUserGameStatusAsync(userId, gameId);
            await NotifyFriendsStatusChanged(userId);
        }
    }

    public async Task StopGame()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            _connectionTracking.UpdateStatus(Context.ConnectionId, UserStatus.Online);
            await _userService.UpdateUserGameStatusAsync(userId, null);
            await NotifyFriendsStatusChanged(userId);
        }
    }

    private async Task NotifyFriendsStatusChanged(string userId)
    {
        try
        {
            // Récupérer le statut actuel de l'utilisateur
            var userConnection = _connectionTracking.GetConnectionByUserId(userId);
            if (userConnection == null) return;

            // Créer l'objet statut à envoyer
            var statusUpdate = new
            {
                userId,
                status = userConnection.CurrentStatus,
                gameId = userConnection.CurrentGameId,
                lastActivity = userConnection.LastActivity
            };

            // Récupérer tous les IDs des amis
            var friendIds = await _userService.GetUserFriendsIdsAsync(userId);

            // Pour chaque ami connecté, envoyer la mise à jour
            foreach (var friendId in friendIds)
            {
                // Envoyer la mise à jour à tous les amis qui sont dans le groupe de l'ami
                await Clients.Group($"user_{friendId}").SendAsync("FriendStatusChanged", statusUpdate);
            }
        }
        catch (Exception ex)
        {
            // Log l'erreur mais ne pas la propager pour ne pas casser la connexion
            Console.WriteLine($"Error notifying friends: {ex.Message}");
        }
    }

    // Méthode helper pour logger les changements de statut
    private async Task LogStatusChange(string userId, UserStatus newStatus, int? gameId = null)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            var statusInfo = gameId.HasValue
                ? $"{newStatus} (Game #{gameId})"
                : newStatus.ToString();

            Console.WriteLine($"User {user?.UserName ?? userId} status changed to {statusInfo}");
        }
        catch
        {
            // Ignorer les erreurs de logging
        }
    }
}
}