using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Http.Headers;

namespace Gauniv.Client.Services
{
    public partial class NetworkService : ObservableObject
    {
        public static NetworkService Instance { get; private set; } = new NetworkService();

        [ObservableProperty]
        private string token;

        public HttpClient httpClient;

        public NetworkService()
        {
            httpClient = new HttpClient();
            Token = null;

            // Observer les changements de token
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Token) && !string.IsNullOrEmpty(Token))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", Token);
                }
            };
        }

        public event Action OnConnected;
    }
}