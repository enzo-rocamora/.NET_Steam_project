using CommunityToolkit.Mvvm.ComponentModel;

namespace Gauniv.Client.Models
{
    public enum DownloadState
    {
        NotStarted,
        Downloading,
        Downloaded,
        Error
    }

    public class GameDownloadInfo : ObservableObject
    {
        private DownloadState _downloadState;
        private double _downloadProgress;
        private string _errorMessage;

        public DownloadState DownloadState
        {
            get => _downloadState;
            set => SetProperty(ref _downloadState, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }
    }
}