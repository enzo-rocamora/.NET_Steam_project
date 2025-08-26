using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using Gauniv.Network.ServerApi;
using Windows.Networking;

namespace Gauniv.Client.ViewModel
{
    public partial class IndexViewModel : ObservableObject
    {
        private readonly ServerApi _serverApi;
        private readonly AuthenticationService _authService;
        private readonly OnlineService _onlineService;

        // Champs d'inscription
        [ObservableProperty]
        private string firstName = string.Empty;

        [ObservableProperty]
        private string lastName = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        // Champs de connexion
        [ObservableProperty]
        private string loginEmail = string.Empty;

        [ObservableProperty]
        private string loginPassword = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public IndexViewModel(ServerApi serverApi, AuthenticationService authService, OnlineService onlineService)
        {
            _serverApi = serverApi;
            _authService = authService;
            _onlineService = onlineService;
        }

        [RelayCommand]
        private async Task Register()
        {
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName))
            {
                ErrorMessage = "Veuillez remplir tous les champs";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Les mots de passe ne correspondent pas";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                // Stocker les identifiants avant l'enregistrement
                string registeredEmail = Email;
                string registeredPassword = Password;

                var request = new RegisterRequest
                {
                    Email = registeredEmail,
                    Password = registeredPassword,
                };

                await _serverApi.RegisterAsync(request);

                await Application.Current.MainPage.DisplayAlert("Succès", "Inscription réussie ! Vous allez maintenant être connecté.", "OK");

                // Utiliser les identifiants stockés pour la connexion automatique
                await _authService.LoginAsync(registeredEmail, registeredPassword);

                // Démarrer la connexion SignalR après le login
                await _onlineService.StartAsync();

                // Réinitialiser les champs après la connexion réussie
                FirstName = string.Empty;
                LastName = string.Empty;
                Email = string.Empty;
                Password = string.Empty;
                ConfirmPassword = string.Empty;

                // Afficher les pages protégées et naviguer vers store
                var appShell = Application.Current.MainPage as AppShell;
                if (appShell != null)
                {
                    await appShell.ShowProtectedPages();
                    await Shell.Current.GoToAsync("//store");
                }
            }
            catch (ApiException ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
            {
                ErrorMessage = "Veuillez remplir tous les champs";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                await _authService.LoginAsync(LoginEmail, LoginPassword);

                // Démarrer la connexion SignalR après le login
                await _onlineService.StartAsync();

                // Réinitialisation des champs
                LoginEmail = string.Empty;
                LoginPassword = string.Empty;

                // Afficher les pages protégées et naviguer vers store
                var appShell = Application.Current.MainPage as AppShell;
                if (appShell != null)
                {
                    await appShell.ShowProtectedPages();
                    await Shell.Current.GoToAsync("//store");
                }
            }
            catch (ApiException ex)
            {
                ErrorMessage = "Échec de la connexion : " + ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = "Erreur de connexion au service de statut : " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}