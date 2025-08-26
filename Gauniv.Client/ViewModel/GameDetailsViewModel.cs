using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;
using System.Globalization;
using System.Windows.Input;

namespace Gauniv.Client.ViewModel
{
    [QueryProperty(nameof(Game), "Game")]
    public partial class GameDetailsViewModel : ObservableObject
    {
        private readonly ServerApi _serverApi;
        private readonly AuthenticationService _authService;
        private readonly StoreViewModel _storeViewModel;
        private readonly GameDownloadService _downloadService;
        private HashSet<int> _ownedGameIds;

        [ObservableProperty]
        private GameDto game;

        [ObservableProperty]
        private bool isOwned;

        [ObservableProperty]
        private bool isDownloading;

        [ObservableProperty]
        private double downloadProgress;

        [ObservableProperty]
        private bool isDownloaded;

        public bool IsNotOwned => !isOwned;

        // Propriétés pour les États des boutons
        public bool IsDownloadButton => IsOwned && !IsDownloaded && !IsDownloading;
        public bool IsPlayButton => IsOwned && IsDownloaded && !IsDownloading;
        public string DownloadProgressText => $"{(DownloadProgress * 100):0}%";

        public ICommand BuyCommand { get; }
        public IAsyncRelayCommand DownloadCommand { get; }
        public IAsyncRelayCommand PlayCommand { get; }

        private CancellationTokenSource _downloadCancellationTokenSource;

        public GameDetailsViewModel(
        ServerApi serverApi,
        AuthenticationService authService,
        StoreViewModel storeViewModel,
        GameDownloadService downloadService)
        {
            _serverApi = serverApi;
            _authService = authService;
            _storeViewModel = storeViewModel;
            _downloadService = downloadService;
            _ownedGameIds = new HashSet<int>();

            game = new GameDto();

            BuyCommand = new AsyncRelayCommand(OnBuyAsync);
            DownloadCommand = new AsyncRelayCommand(DownloadGameAsync);
            PlayCommand = new AsyncRelayCommand(PlayGameAsync);

            LoadOwnedGames();
        }


        partial void OnIsOwnedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotOwned));
        }

        private async void LoadOwnedGames()
        {
            try
            {
                if (_authService.IsAuthenticated)
                {
                    var ownedGames = await _serverApi.GamesAllAsync();
                    _ownedGameIds = new HashSet<int>(ownedGames.Select(g => g.Id));

                    // Mettre à jour IsOwned si Game est déjà défini
                    if (Game != null)
                    {
                        IsOwned = _ownedGameIds.Contains(Game.Id);
                    }
                }
                else
                {
                    _ownedGameIds.Clear();
                    IsOwned = false;
                }
            }
            catch (Exception)
            {
                _ownedGameIds.Clear();
                IsOwned = false;
            }
        }

        private async Task OnBuyAsync()
        {
            if (Game == null) return;

            try
            {
                if (!_authService.IsAuthenticated)
                {
                    var goToLogin = await Shell.Current.DisplayAlert(
                        "Non connecté",
                        "Vous devez être connecté pour acheter un jeu. Voulez-vous vous connecter maintenant ?",
                        "Oui", "Non");

                    if (goToLogin)
                    {
                        await Shell.Current.GoToAsync("//index");
                    }
                    return;
                }

                var confirmPurchase = await Shell.Current.DisplayAlert(
                    "Confirmation d'achat",
                    $"Voulez-vous acheter {Game.Title} pour {Game.Price:C} ?",
                    "Acheter",
                    "Annuler"
                );

                if (!confirmPurchase) return;

                await _serverApi.GamesPOSTAsync(Game.Id);

                // Mettre à jour le statut de possession localement
                _ownedGameIds.Add(Game.Id);
                IsOwned = true;

                // Mettre à jour la liste des jeux dans le StoreViewModel
                await _storeViewModel.RefreshGames();

                // Navigation directe vers MyGames après l'achat
                await Shell.Current.GoToAsync("//mygames", true);
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                if (await _authService.TryRefreshTokenAsync())
                {
                    await OnBuyAsync();
                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Session expirée",
                    "Votre session a expiré. Veuillez vous reconnecter.",
                    "OK"
                );
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//index");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Erreur",
                    $"Une erreur s'est produite lors de l'achat : {ex.Message}",
                    "OK"
                );
            }
        }

        private async Task DownloadGameAsync()
        {
            if (Game == null) return;

            try
            {
                IsDownloading = true;
                _downloadCancellationTokenSource = new CancellationTokenSource();

                var progress = new Progress<double>(p =>
                {
                    DownloadProgress = p;
                    OnPropertyChanged(nameof(DownloadProgressText));
                });

                await _downloadService.DownloadGameAsync(Game.Id, Game.Title, progress, _downloadCancellationTokenSource.Token);
                IsDownloaded = true;
                UpdateButtonStates();
            }
            catch (OperationCanceledException)
            {
                DownloadProgress = 0;
                _downloadService.DeleteGame(Game.Id);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Erreur",
                    $"Erreur lors du téléchargement : {ex.Message}",
                    "OK");
            }
            finally
            {
                IsDownloading = false;
                _downloadCancellationTokenSource?.Dispose();
                _downloadCancellationTokenSource = null;
                UpdateButtonStates();
            }
        }

        private async Task PlayGameAsync()
        {
            if (Game == null) return;

            if (!IsDownloaded)
            {
                var shouldDownload = await Shell.Current.DisplayAlert(
                    "Jeu non téléchargé",
                    $"Voulez-vous télécharger {Game.Title} maintenant ?",
                    "Oui",
                    "Non"
                );

                if (shouldDownload)
                {
                    await DownloadGameAsync();
                }
                return;
            }

            try
            {
                await _serverApi.StartAsync(Game.Id);
                await _downloadService.LaunchGameAsync(Game.Id, () =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdateButtonStates();
                        // Update signal R Status

                    });
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Erreur",
                    $"Impossible de lancer le jeu : {ex.Message}",
                    "OK");
            }
        }

        partial void OnGameChanged(GameDto value)
        {
            if (value != null)
            {
                IsOwned = _ownedGameIds.Contains(value.Id);
                IsDownloaded = _downloadService.IsGameDownloaded(value.Id);
                UpdateButtonStates();
            }
        }

        partial void OnIsDownloadingChanged(bool value)
        {
            UpdateButtonStates();
        }

        partial void OnIsDownloadedChanged(bool value)
        {
            UpdateButtonStates();
        }

        partial void OnDownloadProgressChanged(double value)
        {
            OnPropertyChanged(nameof(DownloadProgressText));
        }

        private void UpdateButtonStates()
        {
            OnPropertyChanged(nameof(IsDownloadButton));
            OnPropertyChanged(nameof(IsPlayButton));
            OnPropertyChanged(nameof(IsNotOwned));
            OnPropertyChanged(nameof(IsOwned));
        }
    }
}