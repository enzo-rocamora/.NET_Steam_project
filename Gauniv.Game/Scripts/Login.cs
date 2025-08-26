using Godot;

public partial class Login : Control
{
    private NetworkManager _network;
    private LocalData _localData;

    private BaseButton _loginButton;
    private Label _errorLabel;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = NetworkManager.Instance;
        _localData = LocalData.Instance;
        
        _loginButton = GetNode<Button>("%ButtonLogin");
        _errorLabel = GetNode<Label>("%ErrorLabel");

        _network.ConnectionStatusChanged += OnConnectionStatusChanged;
        _network.OnAuthenticationResult += OnAuthenticationResult;

        // Initial button state
        _loginButton.Disabled = !_network.IsClientConnected;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public async void _on_login_pressed()
    {
        GD.Print("Login Pressed");
        await _network.AuthenticateAsync(GetNode<LineEdit>("%Username").Text, GetNode<LineEdit>("%Password").Text);
    }

    private void OnConnectionStatusChanged(bool isConnected, string message)
    {
        _loginButton.Disabled = !isConnected;

        if (!isConnected)
        {
            _errorLabel.Visible = true;
            _errorLabel.Text = "Couldn't reach the server !";
        }
        else
        {
            _errorLabel.Visible = false;
        }
    }

    private void OnAuthenticationResult(bool success, Player player, string message)
    {
        if (success)
        {
            GD.Print("Login successful");
            _localData.Player = player;
            GD.Print($"LocalPlayer : {player.Name}, Token : {player.Token}");
            GetTree().ChangeSceneToFile("res://Scenes/ServerBrowser.tscn");
        }
        else
        {
            GD.Print($"Login failed: {message}");
            _loginButton.Disabled = false;
            _errorLabel.Visible = true;
            _errorLabel.Text = message;
        }
    }

    public override void _ExitTree()
    {
        _network.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _network.OnAuthenticationResult -= OnAuthenticationResult;
    }
}
