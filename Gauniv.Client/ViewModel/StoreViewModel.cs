using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Gauniv.Client.ViewModel
{
    public class CategoryViewModel : ObservableObject
    {
        private CategoryDto _category;
        private bool _isSelected;

        public CategoryViewModel(CategoryDto category)
        {
            _category = category;
            IsSelected = false;
        }

        public int Id => _category.Id;
        public string Name => _category.Name;

        public Color BackgroundColor => IsSelected ? Colors.IndianRed : Colors.Gray;
        public Color TextColor => IsSelected ? Colors.White : Colors.Black;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(TextColor));
            }
        }
    }

    public partial class StoreViewModel : ObservableObject
    {
        private readonly ServerApi _serverApi;
        private readonly AuthenticationService _authService;
        private System.Timers.Timer _searchTimer;
        private ObservableCollection<GameDto> _allGames;
        private HashSet<int> _ownedGameIds;

        [ObservableProperty]
        private ObservableCollection<GameDto> games;

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private string searchBarPlaceholder = "Rechercher un jeu...";

        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> categories;

        public ICommand DetailsCommand { get; }
        public ICommand BuyCommand { get; }
        public ICommand ToggleCategoryCommand { get; }
        public ICommand SortByNameCommand { get; }
        public ICommand SortByPriceCommand { get; }
        public ICommand SortByDateCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }

        private bool isNameSortAscending = true;
        private bool isPriceSortAscending = true;
        private bool isDateSortAscending = true;

        [ObservableProperty]
        private string nameSortText = "Nom ↓";

        [ObservableProperty]
        private string priceSortText = "Prix ↓";

        [ObservableProperty]
        private string dateSortText = "Date ↓";

        [ObservableProperty]
        private bool isLoading;



        public StoreViewModel(ServerApi serverApi, AuthenticationService authService)
        {
            _serverApi = serverApi;
            _authService = authService;
            _allGames = new ObservableCollection<GameDto>();
            _ownedGameIds = new HashSet<int>();
            Games = new ObservableCollection<GameDto>();
            Categories = new ObservableCollection<CategoryViewModel>();

            DetailsCommand = new AsyncRelayCommand<GameDto>(OnDetailsAsync);
            BuyCommand = new AsyncRelayCommand<GameDto>(OnBuyAsync);
            ToggleCategoryCommand = new Command<CategoryViewModel>(OnToggleCategory);

            SortByNameCommand = new Command(SortByName);
            SortByPriceCommand = new Command(SortByPrice);
            SortByDateCommand = new Command(SortByDate);

            RefreshCommand = new AsyncRelayCommand(RefreshDataAsync);
            LoadData();
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                IsLoading = true;
                await LoadOwnedGames();
                await LoadStoreGames();
                await LoadCategories();
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

        private void SortByPrice()
        {
            isPriceSortAscending = !isPriceSortAscending;
            PriceSortText = $"Prix {(isPriceSortAscending ? "↑" : "↓")}";
            ApplySort(g => g.Price, isPriceSortAscending);
        }

        private void SortByDate()
        {
            isDateSortAscending = !isDateSortAscending;
            DateSortText = $"Date {(isDateSortAscending ? "↑" : "↓")}";
            ApplySort(g => g.CreatedAt, isDateSortAscending);
        }

        private void ApplySort<T>(Func<GameDto, T> selector, bool ascending)
        {
            var sortedGames = ascending ?
                Games.OrderBy(selector) :
                Games.OrderByDescending(selector);

            Games = new ObservableCollection<GameDto>(sortedGames);
        }

        private async void LoadData()
        {
            await LoadOwnedGames();
            await LoadStoreGames();
            await LoadCategories();
        }

        public async Task RefreshGames()
        {
            await LoadOwnedGames();
            await LoadStoreGames();
        }
        public async Task LoadOwnedGames()
        {
            try
            {
                if (_authService.IsAuthenticated)
                {
                    var ownedGames = await _serverApi.GamesAllAsync();
                    _ownedGameIds = new HashSet<int>(ownedGames.Select(g => g.Id));
                }
                else
                {
                    _ownedGameIds.Clear();
                }
            }
            catch (Exception)
            {
                _ownedGameIds.Clear();
            }
        }

        public async Task LoadStoreGames()
        {
            try
            {
                var response = await _serverApi.GamesGETAsync(0, 50, null);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var availableGames = response.Items.Where(g => !_ownedGameIds.Contains(g.Id));
                    _allGames = new ObservableCollection<GameDto>(availableGames);
                    Games = new ObservableCollection<GameDto>(_allGames);
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", ex.Message, "OK");
            }
        }

        private async Task LoadCategories()
        {
            try
            {
                var categoriesResult = await _serverApi.CategoriesAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Categories = new ObservableCollection<CategoryViewModel>(
                        categoriesResult.Select(c => new CategoryViewModel(c))
                    );
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Erreur", "Impossible de charger les catégories", "OK");
            }
        }

        private void OnToggleCategory(CategoryViewModel category)
        {
            if (category == null) return;

            category.IsSelected = !category.IsSelected;
            ApplyFilters();
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

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        string searchText = SearchText.Trim().ToLower();
                        filteredGames = filteredGames.Where(game =>
                            game.Title?.ToLower().Contains(searchText) ?? false
                        );
                    }

                    var selectedCategories = Categories.Where(c => c.IsSelected).ToList();
                    if (selectedCategories.Any())
                    {
                        var selectedCategoryIds = selectedCategories.Select(c => c.Id).ToList();
                        filteredGames = filteredGames.Where(game =>
                            game.Categories?.Any(category =>
                                selectedCategoryIds.Contains(category.Id)) ?? false
                        );
                    }

                    Games = new ObservableCollection<GameDto>(filteredGames);
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("Erreur", "Erreur lors du filtrage", "OK");
                });
            }
        }

        private async Task OnBuyAsync(GameDto game)
        {
            if (game == null) return;

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
                    $"Voulez-vous acheter {game.Title} pour {game.Price:C} ?",
                    "Acheter",
                    "Annuler"
                );

                if (!confirmPurchase) return;

                await _serverApi.GamesPOSTAsync(game.Id);

                _ownedGameIds.Add(game.Id);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Games.Remove(game);
                    _allGames.Remove(game);
                });

                var goToLibrary = await Shell.Current.DisplayAlert(
                    "Achat réussi",
                    $"{game.Title} a été ajouté à votre bibliothèque. Voulez-vous y accéder maintenant pour le télécharger ?",
                    "Oui",
                    "Non"
                );

                if (goToLibrary)
                {
                    await Shell.Current.GoToAsync("//mygames");
                }
            }
            catch (ApiException ex) when (ex.StatusCode == 401)
            {
                if (await _authService.TryRefreshTokenAsync())
                {
                    await OnBuyAsync(game);
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

        private async Task OnDetailsAsync(GameDto game)
        {
            if (game == null) return;

            var parameters = new Dictionary<string, object>
            {
                { "Game", game }
            };

            try
            {
                await Shell.Current.GoToAsync("gamedetails", parameters);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                await Shell.Current.DisplayAlert("Erreur", "Impossible d'afficher les détails du jeu", "OK");
            }
        }
    }
}