using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gauniv.Client.Services
{
    public class GameDownloadService
    {
        private readonly string _gamesDirectory;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GameDownloadService> _logger;
        private readonly GameProcessManager _processManager;

        public GameDownloadService(
            HttpClient httpClient,
            ILogger<GameDownloadService> logger,
            GameProcessManager processManager)
        {
            _httpClient = httpClient;
            _logger = logger;
            _processManager = processManager;

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string solutionRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
            _gamesDirectory = Path.Combine(solutionRoot, "Games");

            if (!Directory.Exists(_gamesDirectory))
            {
                Directory.CreateDirectory(_gamesDirectory);
            }

            _logger.LogInformation("Games directory initialized at: {Directory}", _gamesDirectory);
        }

        public async Task DownloadGameAsync(int gameId, string gameTitle, IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            var gameDirectory = Path.Combine(_gamesDirectory, gameId.ToString());
            Directory.CreateDirectory(gameDirectory);

            try
            {
                _logger.LogInformation("Starting download for game {GameId}", gameId);

                using var response = await _httpClient.GetAsync(
                    $"api/games/{gameId}/download",
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                );
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength ?? -1L;
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                    ?? $"{gameTitle}.exe".Replace(" ", "_");

                var filePath = Path.Combine(gameDirectory, fileName);
                var totalBytesRead = 0L;
                var buffer = new byte[8192];
                var isMoreToRead = true;

                _logger.LogInformation("Downloading game {GameId} to {FilePath}", gameId, filePath);

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                {
                    do
                    {
                        var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            continue;
                        }

                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

                        totalBytesRead += bytesRead;
                        if (contentLength != -1)
                        {
                            var progressPercentage = (double)totalBytesRead / contentLength;
                            progress.Report(progressPercentage);
                        }
                    }
                    while (isMoreToRead);
                }

                if (contentLength == -1)
                {
                    progress.Report(1.0);
                }

                _logger.LogInformation("Game {GameId} downloaded successfully", gameId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Download cancelled for game {GameId}", gameId);
                Directory.Delete(gameDirectory, true);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download game {GameId}", gameId);
                Directory.Delete(gameDirectory, true);
                throw;
            }
        }

        public async Task<bool> LaunchGameAsync(int gameId, Action onProcessExit)
        {
            var gameDirectory = Path.Combine(_gamesDirectory, gameId.ToString());
            var gameFiles = Directory.GetFiles(gameDirectory, "*.exe");
            
            if (gameFiles.Length == 0)
            {
                _logger.LogError("No executable found in directory {GameDirectory}", gameDirectory);
                throw new FileNotFoundException("No executable found for this game.");
            }

            var gamePath = gameFiles[0];

            try
            {
                _logger.LogInformation("Launching game from {GamePath}", gamePath);
                return await _processManager.LaunchGameAsync(gameId, gamePath, onProcessExit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch game at {GamePath}", gamePath);
                throw;
            }
        }

        public bool StopGame(int gameId)
        {
            return _processManager.StopGame(gameId);
        }

        public bool IsGameDownloaded(int gameId)
        {
            var gameDirectory = Path.Combine(_gamesDirectory, gameId.ToString());
            return Directory.Exists(gameDirectory) && Directory.GetFiles(gameDirectory).Any();
        }

        public void DeleteGame(int gameId)
        {
            var gameDirectory = Path.Combine(_gamesDirectory, gameId.ToString());
            if (Directory.Exists(gameDirectory))
            {
                try
                {
                    Directory.Delete(gameDirectory, true);
                    _logger.LogInformation("Game {GameId} deleted successfully", gameId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete game {GameId}", gameId);
                    throw;
                }
            }
        }

        public string GetGameDirectory(int gameId)
        {
            return Path.Combine(_gamesDirectory, gameId.ToString());
        }
    }
}