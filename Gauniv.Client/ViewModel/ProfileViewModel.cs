using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Gauniv.Client.ViewModel
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly ServerApi _serverApi;
        private readonly OnlineService _onlineService;
        private readonly AuthenticationService _authService;
        private readonly ILogger<ProfileViewModel> _logger;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string newFriendEmail = string.Empty;

        [ObservableProperty]
        private ObservableCollection<FriendViewModel> friends = new();

        public ProfileViewModel(
            ServerApi serverApi,
            OnlineService onlineService,
            AuthenticationService authService,
            ILogger<ProfileViewModel> logger)
        {
            _serverApi = serverApi;
            _authService = authService;
            _onlineService = onlineService;
            _logger = logger;
            
            Task.Run(async () =>
            {
                await LoadUserInfoAsync();
                await LoadFriendsAsync();
            });
        }

        private async Task LoadUserInfoAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var userInfo = await _serverApi.InfoGETAsync();
                Email = userInfo.Email;
            }
            catch (ApiException ex)
            {
                ErrorMessage = $"Error loading profile: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadFriendsAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var friendsList = await _serverApi.FriendsAllAsync();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var friendViewModels = friendsList?
                        .Select(f => new FriendViewModel(f, _onlineService))
                        .ToList() ?? new List<FriendViewModel>();

                    Friends = new ObservableCollection<FriendViewModel>(friendViewModels);
                });
            }
            catch (ApiException ex)
            {
                _logger.LogError(ex, "Error loading friends");
                ErrorMessage = "Unable to load friends list. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading friends");
                ErrorMessage = "An unexpected error occurred. Please try again later.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task AddFriend()
        {
            if (string.IsNullOrWhiteSpace(NewFriendEmail))
            {
                ErrorMessage = "Please enter an email address";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var request = new FriendRequestDto { Email = NewFriendEmail };
                await _serverApi.FriendsAsync(request);

                // Refresh friends list
                await LoadFriendsAsync();
                NewFriendEmail = string.Empty; // Clear input

                await Application.Current.MainPage.DisplayAlert(
                    "Success",
                    "Friend added successfully!",
                    "OK");
            }
            catch (ApiException ex)
            {
                ErrorMessage = $"Error adding friend: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    ex.Message,
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task RefreshFriends()
        {
            await LoadFriendsAsync();
        }
        [RelayCommand]
        private async Task Logout()
        {
            try
            {
                bool answer = await Application.Current.MainPage.DisplayAlert(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Yes",
                    "No");

                if (!answer) return;

                IsLoading = true;

                // Mettre le statut à offline avant de se déconnecter
                try
                {
                    // Arrêter la connexion SignalR
                    await _onlineService.StopAsync();
                }
                catch (Exception ex)
                {
                    // Log l'erreur mais continuer la déconnexion
                    System.Diagnostics.Debug.WriteLine($"Error stopping SignalR connection: {ex.Message}");
                }

                // Se déconnecter de l'API
                await _authService.LogoutAsync();

                var appShell = Application.Current.MainPage as AppShell;
                if (appShell != null)
                {
                    await appShell.HideProtectedPages();
                    await Shell.Current.GoToAsync("//index");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error during logout: {ex.Message}";
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "An error occurred during logout. Please try again.",
                    "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public string GetFriendStatus(int status)
        {
            return status switch
            {
                0 => "Offline",
                1 => "Online",
                2 => "In Game",
                _ => "Unknown"
            };
        }
    }
}