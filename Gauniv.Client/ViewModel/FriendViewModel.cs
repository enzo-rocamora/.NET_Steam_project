using CommunityToolkit.Mvvm.ComponentModel;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;

namespace Gauniv.Client.ViewModel
{
    public partial class FriendViewModel : ObservableObject, IDisposable
    {
        private readonly FriendDto _friendDto;
        private readonly OnlineService _onlineService;

        [ObservableProperty]
        public string id;

        [ObservableProperty]
        public string userName;

        [ObservableProperty]
        public UserStatus status;

        [ObservableProperty]
        public string currentGameTitle;

        [ObservableProperty]
        public bool isPlaying;

        public string StatusText => status switch
        {
            UserStatus.Online => "En ligne",
            UserStatus.Offline => "Hors ligne",
            UserStatus.InGame => $"En jeu - {CurrentGameTitle ?? "Unknown"}",
            _ => "Inconnu"
        };

        public Color StatusColor => status switch
        {
            UserStatus.Online => Colors.Green,
            UserStatus.Offline => Colors.Gray,
            UserStatus.InGame => Colors.Blue,
            _ => Colors.Gray
        };


        private void HandleUserStatusChanged(UserStatusUpdate update)
        {
            // Vérifier si cette mise à jour concerne cet ami
            if (update.UserId != Id) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Status = (UserStatus)update.Status;
                CurrentGameTitle = update.GameName ?? "Error";


                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            });
        }


        public FriendViewModel(FriendDto dto, OnlineService onlineService)
        {
            _onlineService = onlineService;
            _friendDto = dto;
            id = dto.Id;
            userName = dto.UserName ?? "Unknown";
            Status = (UserStatus)dto.Status;
            CurrentGameTitle = dto.CurrentGame?.Title;
            IsPlaying = dto.CurrentGame != null;

            _onlineService.OnUserStatusChanged += HandleUserStatusChanged;
        }
        public void Dispose()
        {
            _onlineService.OnUserStatusChanged -= HandleUserStatusChanged;
        }
    }

    // Redéfinir l'enum pour correspondre aux valeurs du DTO
    public enum UserStatus
    {
        Offline = 0,
        Online = 1,
        InGame = 2
    }
}