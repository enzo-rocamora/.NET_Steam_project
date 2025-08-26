using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using static GameClient;

public partial class NetworkManager : Node
{
    private static NetworkManager _instance;
    private GameClient _client;

    public static NetworkManager Instance => _instance;

    public bool IsClientConnected { get; private set; }

    [Signal]
    public delegate void ConnectionStatusChangedEventHandler(bool isConnected, string message);

    [Signal]
    public delegate void JoinGameResultEventHandler(bool success, string message);

    public event AuthenticationResultEventHandler OnAuthenticationResult
    {
        add
        {
            _client.OnAuthenticationResult += value;
        }
        remove
        {
            _client.OnAuthenticationResult -= value;
        }
    }

    public event CreateGameResultEventHandler OnCreateGameResult
    {
        add
        {
            _client.OnCreateGameResult += value;
        }
        remove
        {
            _client.OnCreateGameResult -= value;
        }
    }

    public event ServerListUpdateEventHandler OnServerListUpdate
    {
        add
        {
            _client.OnServerListUpdate += value;
        }
        remove
        {
            _client.OnServerListUpdate -= value;
        }
    }

    public event PlayerListUpdateEventHandler OnPlayerListUpdate
    {
        add
        {
            _client.OnPlayerListUpdate += value;
        }
        remove
        {
            _client.OnPlayerListUpdate -= value;
        }
    }

    public event StartGameResultEventHandler OnStartGameResult
    {
        add
        {
            _client.OnStartGameResult += value;
        }
        remove
        {
            _client.OnStartGameResult -= value;
        }
    }

    public event GameMasterCellSelectionResponseEventHandler OnGameMasterCellSelectionResponse
    {
        add
        {
            _client.OnGameMasterCellSelectionResponse += value;
        }
        remove
        {
            _client.OnGameMasterCellSelectionResponse -= value;
        }
    }

    public event FinishedGameResponseEventHandler OnFinishedGameResponse
    {
        add
        {
            _client.OnFinishedGameResponse += value;
        }
        remove
        {
            _client.OnFinishedGameResponse -= value;
        }
    }

    [Signal]
    public delegate void GameMasterDisconnectedEventHandler();

    public event GameMasterBoardResponseEventHandler OnGameMasterBoardResponse
    {
        add
        {
            _client.OnGameMasterBoardResponse += value;
        }
        remove
        {
            _client.OnGameMasterBoardResponse -= value;
        }
    }

    public override void _EnterTree()
    {
        _instance = this;

        _client = new GameClient("localhost", 5000);
        _client.Error += (error) =>
            EmitSignal(SignalName.ConnectionStatusChanged, false, error);
        _client.JoinGameResult += (success, message) =>
            EmitSignal(SignalName.JoinGameResult, success, message);
        _client.GameMasterDisconnected += () => EmitSignal(SignalName.GameMasterDisconnected);

        // Try to connect immediately
        _ = ConnectToServerAsync();
    }
    
    private async Task ConnectToServerAsync()
    {
        try
        {
            await _client.ConnectAsync();
            IsClientConnected = true;
            EmitSignal(SignalName.ConnectionStatusChanged, true, "Connected to server");
        }
        catch (Exception ex)
        {
            IsClientConnected = false;
            EmitSignal(SignalName.ConnectionStatusChanged, false, ex.Message);
        }
    }

    public async Task AuthenticateAsync(string username, string password)
    {
        try
        {
            await _client.AuthenticateAsync(username, password);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public async Task RequestServerList()
    {
        try
        {
            await _client.RequestServerList();
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public async Task CreateNewGameRequest(string gameName, int gridColumn, int gridRow)
    {
        try
        {
            await _client.CreateNewGameRequest(gameName, gridColumn, gridRow);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public async Task JoinGameRequest(Guid gameId)
    {
        try
        {
            await _client.JoinGameRequest(gameId);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    internal async Task PlayerReadyRequest(Guid gameId, Guid playerId)
    {
        try
        {
            await _client.PlayerReadyRequest(gameId, playerId);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    internal async Task StartGameRequest(Guid gameId, Guid player)
    {
        try
        {
            await _client.StartGameRequest(gameId, player);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public async Task GameMasterCellSelection(Guid gameId, Guid playerId, (int, int) selectedCell)
    {
        try
        {
            await _client.GameMasterCellSelection(gameId, playerId, selectedCell);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public async Task PlayerCellSelectionResponse(Guid gameId, Guid playerId, Double responseTime)
    {
        try
        {
            await _client.PlayerCellSelectionResponse(gameId, playerId, responseTime);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public async Task GameMasterBoardSelection(Guid gameId, Guid playerId, GridData grid)
    {
        try
        {
            await _client.GameMasterBoardSelection(gameId, playerId, grid);
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _client?.Disconnect();
        }
    }

    public override void _ExitTree()
    {
        try
        {
            GD.Print("Disconnecting client...");
            _client.Disconnect();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error during cleanup: {ex.Message}");
        }
        base._ExitTree();
    }
}
