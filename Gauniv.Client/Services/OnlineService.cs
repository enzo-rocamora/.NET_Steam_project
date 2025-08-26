using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Net.Security;

namespace Gauniv.Client.Services
{
    public class OnlineService : IAsyncDisposable
    {
        private readonly ILogger<OnlineService> _logger;
        private readonly AuthenticationService _authService;
        private HubConnection? _hubConnection;
        private System.Timers.Timer? _keepAliveTimer;
        private const int KEEP_ALIVE_INTERVAL = 10000;
        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        public event Action<UserStatusUpdate>? OnUserStatusChanged;

        public OnlineService(ILogger<OnlineService> logger, AuthenticationService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public async Task UpdateGameStatusAsync(int? gameId)
        {
            if (!IsConnected || _hubConnection == null)
            {
                _logger.LogWarning("Cannot update game status: not connected");
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("UpdateGameStatus", gameId);
                _logger.LogInformation("Game status updated - GameId: {GameId}", gameId?.ToString() ?? "null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update game status to {GameId}", gameId?.ToString() ?? "null");
                throw;
            }
        }

        public async Task StartAsync()
        {
            if (_hubConnection != null)
            {
                return;
            }

            try
            {
                var url = "wss://localhost:7209/online";
                _logger.LogInformation("Creating SignalR connection to {Url}", url);

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url, options =>
                    {
                        // Configuration pour accepter tous les certificats en développement
                        options.WebSocketConfiguration = conf =>
                        {
                            conf.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;
                        };

                        // Forcer l'utilisation de WebSockets
                        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;

                        // Désactiver la négociation automatique
                        options.SkipNegotiation = true;

                        if (_authService.IsAuthenticated)
                        {
                            _logger.LogInformation("Adding authentication token");
                            options.AccessTokenProvider = () => Task.FromResult(_authService.AccessToken);
                        }

                        // Ignorer les erreurs de certificat en développement
                        options.HttpMessageHandlerFactory = handler =>
                        {
                            if (handler is HttpClientHandler clientHandler)
                            {
                                clientHandler.ServerCertificateCustomValidationCallback =
                                    (sender, certificate, chain, errors) => true;
                            }
                            return handler;
                        };
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    })
                    .WithAutomaticReconnect(new[] {
                        TimeSpan.FromSeconds(0),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)
                    })
                    .Build();

                RegisterHandlers();

                _logger.LogInformation("Starting connection...");

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await _hubConnection.StartAsync(cts.Token);

                _logger.LogInformation("Connection started. ConnectionId: {ConnectionId}", _hubConnection.ConnectionId);

                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    try
                    {
                        await _hubConnection.InvokeAsync("SendMessage", "Online", cts.Token);
                        _logger.LogInformation("Initial online status sent successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send initial status");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish SignalR connection");
                await CleanupAsync();
                throw;
            }
        }

        private void RegisterHandlers()
        {
            if (_hubConnection == null) return;

            _hubConnection.On<string>("ReceiveMessage", (message) =>
            {
                _logger.LogInformation("Received message: {Message}", message);
            });

            _hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning(error, "Connection closed");

                if (error != null)
                {
                    _logger.LogError("Connection closed with error: {Error}", error.Message);
                }

                await Task.Delay(5000);
                try
                {
                    await StartAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect");
                }
            };

            _hubConnection.Reconnecting += error =>
            {
                _logger.LogInformation(error, "Attempting to reconnect");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async connectionId =>
            {
                try
                {
                    _logger.LogInformation("Reconnected. New ConnectionId: {ConnectionId}", connectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore online status after reconnection");
                }
            };

            _hubConnection.On<UserStatusUpdate>("UserStatusChanged", (update) =>
            {
                _logger.LogInformation("User {UserId} status changed to {Status} with game {Game}",
                    update.UserId, update.Status, update.GameName ?? "none");

                OnUserStatusChanged?.Invoke(update);
            });
        }

        private async Task CleanupAsync()
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    // Kill the connection, don't send a message
                    await _hubConnection.StopAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send offline status during cleanup");
                }
            }

            if (_hubConnection != null)
            {
                try
                {
                    if (_hubConnection.State != HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StopAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping connection");
                }
                finally
                {
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }
            }
        }

        public async Task StopAsync()
        {
            await CleanupAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }
    }

    public class UserStatusUpdate
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Status { get; set; }
        public int? GameId { get; set; }
        public string? GameName { get; set; }
        public DateTime Timestamp { get; set; }
    }
}