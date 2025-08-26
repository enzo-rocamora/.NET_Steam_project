using Gauniv.Client.Services;

namespace Gauniv.Client
{
    public partial class App : Application
    {
        private readonly AuthenticationService _authService;
        private readonly GameProcessManager _processManager;

        public App(AuthenticationService authService, GameProcessManager processManager)
        {
            InitializeComponent();
            _processManager = processManager;
            _authService = authService;

            // Déconnexion au démarrage
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _authService.LogoutAsync();
            });

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            window.Destroying += Window_Destroying;

            return window;
        }

        private void Window_Destroying(object sender, EventArgs e)
        {
            _processManager.Cleanup();
        }
    }
}