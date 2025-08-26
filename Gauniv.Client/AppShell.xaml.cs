using Gauniv.Client.Pages;

namespace Gauniv.Client
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Enregistrement des routes
            Routing.RegisterRoute("gamedetails", typeof(GameDetails));

            // Vérification du token au démarrage
            CheckAuthenticationStatus();
        }

        private async void CheckAuthenticationStatus()
        {
            var token = await SecureStorage.Default.GetAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                // Si token présent, afficher les pages protégées et rediriger vers store
                await ShowProtectedPages();
            }
        }

        public async Task ShowProtectedPages()
        {
            MainTabBar.IsVisible = true;
            FlyoutBehavior = FlyoutBehavior.Flyout;
            await GoToAsync("///store");
        }

        public async Task HideProtectedPages()
        {
            MainTabBar.IsVisible = false;
            FlyoutBehavior = FlyoutBehavior.Disabled;
            await GoToAsync("///index");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Déconnexion", "Êtes-vous sûr de vouloir vous déconnecter ?", "Oui", "Non");

            if (answer)
            {
                await Logout();
            }
        }

        public async Task Logout()
        {
            // Suppression des tokens
            SecureStorage.Default.Remove("access_token");
            SecureStorage.Default.Remove("refresh_token");

            // Cacher les pages protégées et retourner à l'index
            await HideProtectedPages();
        }
    }
}