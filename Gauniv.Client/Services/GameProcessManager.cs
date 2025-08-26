using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gauniv.Client.Services
{
    public class GameProcessManager
    {
        private readonly Dictionary<int, (Process Process, CancellationTokenSource CancellationSource)> _runningProcesses = new();
        private readonly Dictionary<int, Action> _processExitCallbacks = new();
        private readonly ILogger<GameProcessManager> _logger;

        public GameProcessManager(ILogger<GameProcessManager> logger)
        {
            _logger = logger;
        }

        public async Task<bool> LaunchGameAsync(int gameId, string executablePath, Action onProcessExit)
        {
            if (_runningProcesses.ContainsKey(gameId))
            {
                _logger.LogWarning("Process already running for game {GameId}", gameId);
                return false;
            }

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(executablePath)
                    },
                    EnableRaisingEvents = true
                };

                var cts = new CancellationTokenSource();
                _processExitCallbacks[gameId] = onProcessExit;

                process.Exited += (sender, args) => HandleProcessExit(gameId);
                process.Start();

                _runningProcesses[gameId] = (process, cts);

                // Surveillance du processus dans un thread séparé
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await process.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Process monitoring cancelled for game {GameId}", gameId);
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogInformation("Process already exited for game {GameId}", gameId);
                    }
                    finally
                    {
                        if (!cts.Token.IsCancellationRequested)
                        {
                            HandleProcessExit(gameId);
                        }
                    }
                }, cts.Token);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch game {GameId}", gameId);
                return false;
            }
        }

        private void HandleProcessExit(int gameId)
        {
            if (_runningProcesses.TryGetValue(gameId, out var processInfo))
            {
                try
                {
                    if (!processInfo.Process.HasExited)
                    {
                        KillProcess(processInfo.Process);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing process for game {GameId}", gameId);
                }
                finally
                {
                    processInfo.CancellationSource.Cancel();
                    processInfo.Process.Dispose();
                    _runningProcesses.Remove(gameId);

                    if (_processExitCallbacks.TryGetValue(gameId, out var callback))
                    {
                        MainThread.BeginInvokeOnMainThread(callback);
                        _processExitCallbacks.Remove(gameId);
                    }
                }
            }
        }

        private void KillProcess(Process process)
        {
            try
            {
                _logger.LogInformation("Attempting to kill process {ProcessId}", process.Id);

                // Tenter de tuer le processus et ses enfants de manière forcée
                process.Kill(entireProcessTree: true);

                _logger.LogInformation("Kill command sent to process {ProcessId}, waiting for exit", process.Id);

                // Attendre jusqu'à 5 secondes que le processus se termine
                if (!process.WaitForExit(5000))
                {
                    _logger.LogWarning("Process {ProcessId} did not exit within timeout period, forcing kill", process.Id);
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        _logger.LogInformation("Forced kill successful for process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to force kill process {ProcessId}", process.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Process {ProcessId} successfully terminated", process.Id);
                }
            }
            catch (InvalidOperationException)
            {
                _logger.LogInformation("Process has already exited");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill process");
                throw;
            }
        }

        public bool StopGame(int gameId)
        {
            _logger.LogInformation("StopGame called for game {GameId}", gameId);

            if (!_runningProcesses.TryGetValue(gameId, out var processInfo))
            {
                _logger.LogWarning("No running process found for game {GameId}", gameId);
                return false;
            }

            try
            {
                if (!processInfo.Process.HasExited)
                {
                    _logger.LogInformation("Killing process for game {GameId}", gameId);
                    KillProcess(processInfo.Process);
                    _logger.LogInformation("Successfully killed process for game {GameId}", gameId);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop game {GameId}", gameId);
                return false;
            }
            finally
            {
                _logger.LogInformation("Cleaning up process resources for game {GameId}", gameId);
                HandleProcessExit(gameId);
            }
        }

        public bool IsGameRunning(int gameId)
        {
            return _runningProcesses.TryGetValue(gameId, out var processInfo)
                   && !processInfo.Process.HasExited;
        }

        public void Cleanup()
        {
            foreach (var gameId in _runningProcesses.Keys.ToList())
            {
                StopGame(gameId);
            }
        }
    }
}