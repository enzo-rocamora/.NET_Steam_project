using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;

namespace Gauniv.Client.Models
{
    public partial class GameWithDownload : ObservableObject
    {
        private readonly GameDownloadService _downloadService;
        private CancellationTokenSource? _downloadCancellationTokenSource;
        private System.Timers.Timer? _runningTimer;
        private Process? _currentProcess;
        private readonly OnlineService _onlineService;

        [ObservableProperty]
        private double downloadProgress;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private bool isRunning;

        [ObservableProperty]
        private DateTime? startTime;

        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string TotalPlayTime { get; set; }
        public DateTime? LastPlayedAt { get; set; }
        public ICollection<string> Categories { get; set; }

        public string DownloadProgressText => $"{(DownloadProgress * 100):0}%";
        public bool IsDownloadButton => !IsDownloaded && !IsDownloading && !IsRunning;
        public bool IsPlayButton => IsDownloaded && !IsRunning && !IsDownloading;
        public bool IsDownloaded => _downloadService.IsGameDownloaded(Id);
        public bool ShowRunningState => IsRunning && !IsDownloading;
        public string RunningTime => StartTime.HasValue
            ? (DateTime.Now - StartTime.Value).ToString(@"hh\:mm\:ss")
            : string.Empty;

        public GameWithDownload(UserGameDto gameDto, GameDownloadService downloadService, OnlineService onlineService)
        {
            _downloadService = downloadService;

            Id = gameDto.Id;
            Title = gameDto.Title;
            PurchaseDate = gameDto.PurchaseDate.DateTime;
            TotalPlayTime = gameDto.TotalPlayTime ?? "0h";
            LastPlayedAt = gameDto.LastPlayedAt?.DateTime;
            Categories = gameDto.Categories;

            downloadProgress = IsDownloaded ? 1 : 0;
            isRunning = false;
            startTime = null;
            _onlineService = onlineService;
        }

        partial void OnDownloadProgressChanged(double value)
        {
            OnPropertyChanged(nameof(DownloadProgressText));
            OnPropertyChanged(nameof(IsDownloadButton));
            OnPropertyChanged(nameof(IsPlayButton));
            OnPropertyChanged(nameof(ShowRunningState));
        }

        partial void OnIsDownloadingChanged(bool value)
        {
            OnPropertyChanged(nameof(IsDownloadButton));
            OnPropertyChanged(nameof(IsPlayButton));
            OnPropertyChanged(nameof(ShowRunningState));
        }

        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(IsDownloadButton));
            OnPropertyChanged(nameof(IsPlayButton));
            OnPropertyChanged(nameof(ShowRunningState));
        }

        public async Task StartDownloadAsync(CancellationToken cancellationToken = default)
        {
            if (IsDownloaded || IsDownloading) return;

            try
            {
                IsDownloading = true;
                _downloadCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var progress = new Progress<double>(p => DownloadProgress = p);
                await _downloadService.DownloadGameAsync(Id, Title, progress, _downloadCancellationTokenSource.Token);

                DownloadProgress = 1;
            }
            catch (OperationCanceledException)
            {
                DownloadProgress = 0;
                _downloadService.DeleteGame(Id);
            }
            finally
            {
                IsDownloading = false;
                _downloadCancellationTokenSource?.Dispose();
                _downloadCancellationTokenSource = null;
            }
        }

        public async Task<bool> LaunchGameAsync()
        {
            if (!IsDownloaded) return false;

            try
            {
                var launched = await _downloadService.LaunchGameAsync(Id, () =>
                {
                    StopRunningTimer();
                    IsRunning = false;
                    _currentProcess = null;
                    _onlineService.UpdateGameStatusAsync(null);
                });

                if (launched)
                {
                    StartRunningTimer();
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void StopRunningTimer()
        {
            IsRunning = false;
            StartTime = null;
            _runningTimer?.Stop();
            _runningTimer?.Dispose();
            _runningTimer = null;

            OnPropertyChanged(nameof(IsPlayButton));
            OnPropertyChanged(nameof(IsDownloadButton));
            OnPropertyChanged(nameof(ShowRunningState));
        }

        public void StartRunningTimer()
        {
            IsRunning = true;
            StartTime = DateTime.Now;

            _runningTimer = new System.Timers.Timer(1000);
            _runningTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(RunningTime));
                });
            };
            _runningTimer.Start();

            OnPropertyChanged(nameof(IsPlayButton));
            OnPropertyChanged(nameof(IsDownloadButton));
            OnPropertyChanged(nameof(ShowRunningState));
        }

        public void CancelDownload()
        {
            _downloadCancellationTokenSource?.Cancel();
        }
    }
}