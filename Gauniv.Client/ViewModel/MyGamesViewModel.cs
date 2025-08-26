using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Models;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Gauniv.Client.ViewModel;
using System.Windows.Input;

namespace Gauniv.Client.ViewModels
{
    public partial class MyGamesViewModel : ObservableObject
    {
        private readonly ServerApi _serverApi;
        private readonly GameDownloadService _downloadService;
        private readonly ILogger<MyGamesViewModel> _logger;
        private readonly AuthenticationService _authService;
        private readonly GameProcessManager _processManager;
        private readonly OnlineService _onlineService;

        [ObservableProperty]
        private ObservableCollection<GameWithDownload> games;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> categories;

        [ObservableProperty]
        private ObservableCollection<GameWithDownload> filteredGames;

        [ObservableProperty]
        private string nameSortText = "Nom ↓";

        [ObservableProperty]
        private string dateSortText = "Date d'achat ↓";


        public IAsyncRelayCommand<GameWithDownload> DownloadCommand { get; }
        public IAsyncRelayCommand<GameWithDownload> PlayCommand { get; }
        public IAsyncRelayCommand<GameWithDownload> StopGameCommand { get; }
        public IAsyncRelayCommand<GameWithDownload> DetailsCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByPlayTimeCommand { get; }
        public ICommand SortByDateCommand { get; }

        private ObservableCollection<GameWithDownload> _allGames;
        private System.Timers.Timer _searchTimer;

        private bool isNameSortAscending = true;
        private bool isDateSortAscending = true;


        public MyGamesViewModel(
        ServerApi serverApi,
        GameDownloadService downloadService,
        AuthenticationService authService,
        GameProcessManager processManager,
        OnlineService onlineService,
        ILogger<MyGamesViewModel> logger)
        {
            _serverApi = serverApi;
            _downloadService = downloadService;
            _authService = authService;
            _processManager = processManager;
            _onlineService = onlineService;
            _logger = logger;

            Games = new ObservableCollection<GameWithDownload>();

            DownloadCommand = new AsyncRelayCommand<GameWithDownload>(DownloadGameAsync);
            PlayCommand = new AsyncRelayCommand<GameWithDownload>(PlayGameAsync);
            StopGameCommand = new AsyncRelayCommand<GameWithDownload>(StopGameAsync);
            DetailsCommand = new AsyncRelayCommand<GameWithDownload>(OnDetailsAsync);
            RefreshCommand = new AsyncRelayCommand(LoadGamesAndCategories);
            _allGames = new ObservableCollection<GameWithDownload>();
            FilteredGames = new ObservableCollection<GameWithDownload>();
            Categories = new ObservableCollection<CategoryViewModel>();

            SortByNameCommand = new Command(SortByName);
            SortByDateCommand = new Command(SortByDate);

            LoadGamesAndCategories();
        }

        private async Task LoadGamesAndCategories()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Load games
                var userGames = await _serverApi.GamesAllAsync();
                var games = userGames.Select(g => new GameWithDownload(g, _downloadService, _onlineService)).ToList();

                // Load categories
                var categories = await _serverApi.CategoriesAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _allGames = new ObservableCollection<GameWithDownload>(games);
                    Games = new ObservableCollection<GameWithDownload>(games); // Add this line
                    Categories = new ObservableCollection<CategoryViewModel>(
                        categories.Select(c => new CategoryViewModel(c))
                    );
                    ApplyFilters();
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load your games. Please try again later.";
                _logger.LogError(ex, "Failed to load games and categories");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SortByName()
        {
            isNameSortAscending = !isNameSortAscending;
            NameSortText = $"Nom {(isNameSortAscending ? "↑" : "↓")}";
            ApplySort(g => g.Title, isNameSortAscending);
        }

        private void SortByDate()
        {
            isDateSortAscending = !isDateSortAscending;
            DateSortText = $"Date d'achat {(isDateSortAscending ? "↑" : "↓")}";
            ApplySort(g => g.PurchaseDate, isDateSortAscending);
        }

        private void ApplySort<T>(Func<GameWithDownload, T> selector, bool ascending)
        {
            var sortedGames = ascending ?
                FilteredGames.OrderBy(selector) :
                FilteredGames.OrderByDescending(selector);

            FilteredGames = new ObservableCollection<GameWithDownload>(sortedGames);
            Games = FilteredGames; // Add this line to keep both collections in sync
        }

        partial void OnSearchTextChanged(string value)
        {
            if (_searchTimer != null)
                _searchTimer.Stop();

            _searchTimer = new System.Timers.Timer(500);
            _searchTimer.Elapsed += async (s, e) =>
            {
                _searchTimer.Stop();
                ApplyFilters();
            };
            _searchTimer.Start();
        }

        private void ApplyFilters()
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var filteredGames = _allGames.AsEnumerable();

                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        string searchText = SearchText.Trim().ToLower();
                        filteredGames = filteredGames.Where(game =>
                            game.Title.ToLower().Contains(searchText)
                        );
                    }

                    // Apply category filters
                    var selectedCategories = Categories.Where(c => c.IsSelected).ToList();
                    if (selectedCategories.Any())
                    {
                        var selectedCategoryNames = selectedCategories.Select(c => c.Name).ToList();
                        filteredGames = filteredGames.Where(game =>
                            game.Categories != null &&
                            game.Categories.Any(category =>
                                selectedCategoryNames.Contains(category))
                        );
                    }

                    Games = new ObservableCollection<GameWithDownload>(filteredGames);
                    FilteredGames = Games; // Gardez les deux collections synchronisées
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying filters");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to apply filters", "OK");
                });
            }
        }

        private async Task DownloadGameAsync(GameWithDownload game)
        {
            if (game == null) return;

            try
            {
                await game.StartDownloadAsync();
                _logger.LogInformation("Successfully downloaded game {GameId}: {Title}", game.Id, game.Title);
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                _logger.LogWarning(ex, "Authentication expired while downloading game {GameId}", game.Id);

                if (await _authService.TryRefreshTokenAsync())
                {
                    await DownloadGameAsync(game);
                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Session Expired",
                    "Please log in again to continue downloading.",
                    "OK"
                );
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download game {GameId}: {Title}", game.Id, game.Title);

                await Shell.Current.DisplayAlert(
                    "Download Failed",
                    $"Could not download {game.Title}: {ex.Message}",
                    "OK"
                );
            }
        }

        private async Task PlayGameAsync(GameWithDownload game)
        {
            if (game == null) return;

            try
            {
                if (!game.IsDownloaded)
                {
                    var shouldDownload = await Shell.Current.DisplayAlert(
                        "Game Not Downloaded",
                        $"Would you like to download {game.Title} now?",
                        "Yes",
                        "No"
                    );

                    if (shouldDownload)
                    {
                        await DownloadGameAsync(game);
                    }
                    return;
                }

                // Mettre à jour le statut avant le lancement du jeu
                await _onlineService.UpdateGameStatusAsync(game.Id);

                // Notify server about game start
                await _serverApi.StartAsync(game.Id);

                var launched = await game.LaunchGameAsync();
                if (!launched)
                {
                    // Si le lancement échoue, revenir au statut Online
                    await _onlineService.UpdateGameStatusAsync(null);
                    throw new Exception("Failed to launch game executable");
                }

                _logger.LogInformation("Successfully launched game {GameId}: {Title}", game.Id, game.Title);
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                _logger.LogWarning(ex, "Authentication expired while launching game {GameId}", game.Id);

                if (await _authService.TryRefreshTokenAsync())
                {
                    await PlayGameAsync(game);
                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Session Expired",
                    "Please log in again to play games.",
                    "OK"
                );
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to launch game {GameId}: {Title}", game.Id, game.Title);

                // En cas d'erreur, s'assurer que le statut revient à Online


                await Shell.Current.DisplayAlert(
                    "Launch Failed",
                    $"Could not launch {game.Title}: {ex.Message}",
                    "OK"
                );
            }
        }

        private async Task StopGameAsync(GameWithDownload game)
        {
            if (game == null) return;

            try
            {
                _logger.LogInformation("Attempting to stop game {GameId}: {Title}", game.Id, game.Title);

                // Notify server
                await _serverApi.StopAsync();

                // Stop the local process
                var stopped = _processManager.StopGame(game.Id);
                if (stopped)
                {
                    game.StopRunningTimer();
                    _logger.LogInformation("Successfully stopped game {GameId}", game.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to stop game {GameId}, process not found or already stopped", game.Id);
                }
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                if (await _authService.TryRefreshTokenAsync())
                {
                    await StopGameAsync(game);
                    return;
                }

                await Shell.Current.DisplayAlert(
                    "Session Expired",
                    "Please log in again.",
                    "OK"
                );
                await _authService.LogoutAsync();
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop game {GameId}", game.Id);

                await Shell.Current.DisplayAlert(
                    "Error",
                    $"Could not stop the game: {ex.Message}",
                    "OK"
                );
            }
        }

        private async Task OnDetailsAsync(GameWithDownload game)
        {
            if (game == null) return;

            // Créer un GameDto à partir du GameWithDownload
            var gameDto = new GameDto
            {
                Id = game.Id,
                Title = game.Title,
                Categories = game.Categories.Select(c => new CategoryDto { Name = c }).ToList(),
                CreatedAt = game.PurchaseDate,
            };

            var parameters = new Dictionary<string, object>
        {
            { "Game", gameDto }
        };

            try
            {
                await Shell.Current.GoToAsync("gamedetails", parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation error to game details for {GameId}", game.Id);
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'afficher les détails du jeu", "OK");
            }
        }
    }
}