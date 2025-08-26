using CommunityToolkit.Maui;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using Gauniv.Client.ViewModel;
using Gauniv.Client.ViewModels;
using Gauniv.Network.ServerApi;
using Microsoft.Extensions.Logging;

namespace Gauniv.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>().UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var handler = new HttpClientHandler();
                // Accepter tous les certificats en développement
#if DEBUG
                handler.ServerCertificateCustomValidationCallback =
                    (message, cert, chain, errors) => true;
#endif

                var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:7209")
                };

                return client;
            });

            // Enregistrer ServerApi
            builder.Services.AddSingleton<ServerApi>(sp =>
            {
                var httpClient = sp.GetRequiredService<HttpClient>();
                return new ServerApi(httpClient);
            });

            // Enregistrer AuthenticationService
            builder.Services.AddSingleton<AuthenticationService>();

            // Register 
            builder.Services.AddSingleton<ServerApi>();
            builder.Services.AddTransient<StoreViewModel>();
            builder.Services.AddSingleton<NetworkService>();
            builder.Services.AddSingleton<OnlineService>();
            builder.Services.AddSingleton<GameDownloadService>();
            builder.Services.AddSingleton<GameProcessManager>();
            builder.Services.AddTransient<IndexViewModel>();
            builder.Services.AddTransient<GameDetailsViewModel>();
            builder.Services.AddTransient<Pages.Index>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<Profile>();
            builder.Services.AddTransient<Store>();
            builder.Services.AddTransient<GameDetails>();
            builder.Services.AddTransient<MyGamesViewModel>();
            builder.Services.AddTransient<MyGames>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            Task.Run(() =>
            {
                // Vous pouvez initialiser la connection au serveur a partir d'ici
            });
            return app;
        }
    }
}
