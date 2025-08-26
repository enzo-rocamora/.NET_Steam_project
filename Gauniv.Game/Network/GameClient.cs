using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Godot;
using MessagePack;

public partial class GameClient : Node
{
    private TcpClient _client;
    private NetworkStream _stream;
    private readonly string _serverAddress;
    private readonly int _serverPort;
    private bool _connected;

    [Signal]
    public delegate void ErrorEventHandler(string error);

    public delegate void AuthenticationResultEventHandler(bool success, Player player, string message);
    public event AuthenticationResultEventHandler OnAuthenticationResult;

    public delegate void ServerListUpdateEventHandler(List<GameInfo> servers);
    public event ServerListUpdateEventHandler OnServerListUpdate;

    public delegate void PlayerListUpdateEventHandler(Dictionary<Guid, Player> Players);
    public event PlayerListUpdateEventHandler OnPlayerListUpdate;

    [Signal]
    public delegate void JoinGameResultEventHandler(bool success, string message);

    public delegate void CreateGameResultEventHandler(bool success, GameInfo game, string message);
    public event CreateGameResultEventHandler OnCreateGameResult;

    public delegate void StartGameResultEventHandler(bool success, GameInfo game, string message);
    public event StartGameResultEventHandler OnStartGameResult;

    public delegate void GameMasterCellSelectionResponseEventHandler(Guid gameId, (int, int) selectedCell);
    public event GameMasterCellSelectionResponseEventHandler OnGameMasterCellSelectionResponse;

    public delegate void FinishedGameResponseEventHandler(Guid gameId, List<GameResult> gameResult);
    public event FinishedGameResponseEventHandler OnFinishedGameResponse;

    [Signal]
    public delegate void GameMasterDisconnectedEventHandler();

    public delegate void GameMasterBoardResponseEventHandler(Guid gameId, GridData gameGrid);
    public event GameMasterBoardResponseEventHandler OnGameMasterBoardResponse;

    public GameClient(string serverAddress, int serverPort)
    {
        _serverAddress = serverAddress;
        _serverPort = serverPort;
    }

    public async Task ConnectAsync()
    {
        while (!_connected)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverAddress, _serverPort);
                _stream = _client.GetStream();
                _connected = true;

                _ = ListenForMessagesAsync();
            }
            catch (Exception ex)
            {
                EmitSignal(SignalName.Error, $"Failed to connect: {ex.Message}");
                await Task.Delay(5000);
            }
        }
    }

    private async Task ListenForMessagesAsync()
    {
        while (_connected)
        {
            try
            {
                using var streamReader = new MessagePackStreamReader(_stream);
                while (await streamReader.ReadAsync(default) is ReadOnlySequence<byte> msgpack)
                {
                    HandleMessage(MessagePackSerializer.Deserialize<IGameMessage>(msgpack, cancellationToken: default));
                }
            }
            catch (Exception ex)
            {
                EmitSignal(SignalName.Error, $"Failed to connect: {ex.Message}");
                break;
            }
        }
    }

    void HandleMessage(IGameMessage message)
    {
        try
        {
            switch (message)
            {
                case AuthenticationResponse m:
                    OnAuthenticationResult?.Invoke(m.Success, m.Player, m.Message);
                    GD.Print($"Authentication response: {m.Success}, {m.Player}, {m.Message}");
                    break;

                case ServerListResponse m:
                    OnServerListUpdate?.Invoke(m.Servers);
                    GD.Print($"Server List response: {m.Servers.Count} servers found");
                    break;

                case JoinGameResponse m:
                    EmitSignal(SignalName.JoinGameResult, m.Success, m.Message);
                    GD.Print($"Join Game response: {m.Success}, {m.Message}");
                    break;

                case CreateNewGameResponse m:
                    OnCreateGameResult?.Invoke(m.Success, m.Game, m.Message);
                    GD.Print($"Create Game response: {m.Success}, {m.Game}, {m.Message}");
                    break;

                case PlayerListResponse m:
                    OnPlayerListUpdate?.Invoke(m.Players);
                    GD.Print($"Player List Update: {m.Players.Count} players in game");
                    break;

                case StartGameResponse m:
                    OnStartGameResult?.Invoke(m.Success, m.Game, m.Message);
                    GD.Print($"Start Game response: {m.Success}, {m.Message}");
                    break;

                case GameMasterCellSelectionResponse m:
                    OnGameMasterCellSelectionResponse?.Invoke(m.GameId, m.SelectedCell);
                    GD.Print($"Game Master Cell Selection response: {m.GameId}, {m.SelectedCell}");
                    break;

                case FinishedGameResponse m:
                    OnFinishedGameResponse?.Invoke(m.GameId, m.GameResult);
                    GD.Print("Received Game Result");
                    break;

                case GameMasterBoardResponse m:
                    OnGameMasterBoardResponse?.Invoke(m.GameId, m.GameGrid);
                    GD.Print("Received Game Master Board Selection");
                    break;

                case DisconnectPlayer m:
                    EmitSignal(SignalName.GameMasterDisconnected);
                    GD.Print("GameMaster Disconnected!");
                    break;

                default:
                    Console.WriteLine($"Unknown message received from server");
                    break;
            }
        }
        catch (Exception ex)
        {
            GD.Print(ex);
        }
    }

    private async Task SendMessageAsync(IGameMessage message)
    {
        if (!_connected)
        {
            EmitSignal(SignalName.Error, "Not connected to server");
            return;
        }

        try
        {
            var data = MessagePackSerializer.Serialize(message);
            await _stream.WriteAsync(data);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            EmitSignal(SignalName.Error, $"Error sending message: {ex.Message}");
            throw;
        }
    }

    public async Task AuthenticateAsync(string username, string password)
    {
        var request = new AuthenticationRequest
        {
            Username = username,
            Password = password
        };

        await SendMessageAsync(request);
    }

    public async Task RequestServerList()
    {
        var request = new ServerListRequest { };
        await SendMessageAsync(request);
    }

    public async Task CreateNewGameRequest(string gameName, int gridColumn, int gridRow)
    {
        var request = new CreateNewGameRequest
        {
            GameName = gameName,
            GridColumn = gridColumn,
            GridRow = gridRow
        };
        await SendMessageAsync(request);
    }

    internal async Task JoinGameRequest(Guid gameId)
    {
        var request = new JoinGameRequest
        {
            GameId = gameId
        };
        await SendMessageAsync(request);
    }

    internal async Task PlayerReadyRequest(Guid gameId, Guid playerId)
    {
        var request = new PlayerReadyRequest
        {
            GameId = gameId,
            PlayerId = playerId
        };
        await SendMessageAsync(request);
    }

    internal async Task StartGameRequest(Guid gameId, Guid playerId)
    {
        var request = new StartGameRequest
        {
            GameId = gameId,
            PlayerId = playerId
        };
        await SendMessageAsync(request);
    }

    public async Task GameMasterCellSelection(Guid gameId, Guid playerId, (int, int) selectedCell)
    {
        var request = new GameMasterCellSelection
        {
            GameId = gameId,
            PlayerId = playerId,
            SelectedCell = selectedCell
        };
        await SendMessageAsync(request);
    }

    public async Task PlayerCellSelectionResponse(Guid gameId, Guid playerId, Double responseTime)
    {
        var request = new PlayerCellSelectionResponse
        {
            GameId = gameId,
            PlayerId = playerId,
            ResponseTime = responseTime
        };
        await SendMessageAsync(request);
    }

    public async Task GameMasterBoardSelection(Guid gameId, Guid playerId, GridData grid)
    {
        var request = new GameMasterBoardSelection
        {
            GameId = gameId,
            PlayerId = playerId,
            GameGrid = grid
        };
        await SendMessageAsync(request);
    }

    public void Disconnect()
    {
        GD.Print("Disconnecting Client!");
        _connected = false;
        _stream?.Close();
        _client?.Close();
    }
}
