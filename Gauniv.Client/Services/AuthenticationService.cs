using Gauniv.Network.ServerApi;

public class AuthenticationService
{
    private readonly ServerApi _serverApi;
    private readonly HttpClient _httpClient;
    private const string ACCESS_TOKEN_KEY = "access_token";
    private const string REFRESH_TOKEN_KEY = "refresh_token";
    private const string USER_EMAIL_KEY = "user_email";

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? UserEmail { get; private set; }

    public AuthenticationService(ServerApi serverApi, HttpClient httpClient)
    {
        _serverApi = serverApi;
        _httpClient = httpClient;
    }

    public async Task LoginAsync(string email, string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response = await _serverApi.LoginAsync(false, false, request);

        await SecureStorage.Default.SetAsync(ACCESS_TOKEN_KEY, response.AccessToken);
        await SecureStorage.Default.SetAsync(REFRESH_TOKEN_KEY, response.RefreshToken);
        Preferences.Set(USER_EMAIL_KEY, email);

        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        UserEmail = email;

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);
    }

    public async Task LogoutAsync()
    {
        // Supprimer les tokens stockés
        SecureStorage.Default.Remove(ACCESS_TOKEN_KEY);
        SecureStorage.Default.Remove(REFRESH_TOKEN_KEY);
        Preferences.Remove(USER_EMAIL_KEY);

        // Réinitialiser les propriétés
        AccessToken = null;
        RefreshToken = null;
        UserEmail = null;

        // Supprimer l'en-tête d'autorisation
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(RefreshToken))
                return false;

            var request = new RefreshRequest { RefreshToken = RefreshToken };
            var response = await _serverApi.RefreshAsync(request);

            await SecureStorage.Default.SetAsync(ACCESS_TOKEN_KEY, response.AccessToken);
            await SecureStorage.Default.SetAsync(REFRESH_TOKEN_KEY, response.RefreshToken);

            AccessToken = response.AccessToken;
            RefreshToken = response.RefreshToken;

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AccessToken);

            return true;
        }
        catch
        {
            await LogoutAsync();
            return false;
        }
    }
}