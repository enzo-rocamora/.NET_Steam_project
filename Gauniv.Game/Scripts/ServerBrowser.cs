using System.Collections.Generic;
using Godot;

public partial class ServerBrowser : Control
{
    private PackedScene _createGamePopupScene;
    private PackedScene _connectGamePopupScene;
    private BaseButton _buttonCreate;

    private NetworkManager _network;
    private LocalData _localData;
    private ItemList _serverBrowser;

    private List<GameInfo> _serverList;
    private GameInfo _selectedGame;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = NetworkManager.Instance;
        _serverBrowser = GetNode<ItemList>("%ServerList");

        _localData = LocalData.Instance;

        _network.ConnectionStatusChanged += OnConnectionStatusChanged;
        _network.OnServerListUpdate += OnServerListUpdate;
        _network.OnCreateGameResult += OnCreateGameResult;
        _network.JoinGameResult += OnJoinGameResult;

        RequestServerList();
    }

    private void _network_GameMasterDisconnected()
    {
        throw new System.NotImplementedException();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void _on_button_create_pressed()
    {
        _createGamePopupScene = GD.Load<PackedScene>("res://Scenes/CreateGamePopup.tscn");
        var createGamePopup = _createGamePopupScene.Instantiate();
        AddChild(createGamePopup);
    }

    public async void _on_button_connect_pressed()
    {
        GD.Print($"Connecting to game : {_selectedGame.GameId}, {_selectedGame.GameName}");
        await _network.JoinGameRequest(_selectedGame.GameId);
    }

    private void OnCreateGameResult(bool success, GameInfo game, string message)
    {
        if (success)
        {
            _connectGamePopupScene = GD.Load<PackedScene>("res://Scenes/ConnectGamePopup.tscn");
            var connecteGamePopup = _connectGamePopupScene.Instantiate();

            AddChild(connecteGamePopup);

            _localData.Game = game;
        }
        else
        {
            GD.Print($"Create Game failed: {message}");
        }
    }

    private void OnJoinGameResult(bool result, string message)
    {
        if (result)
        {
            _connectGamePopupScene = GD.Load<PackedScene>("res://Scenes/ConnectGamePopup.tscn");
            var connecteGamePopup = _connectGamePopupScene.Instantiate();

            AddChild(connecteGamePopup);

            _localData.Player.Ready = false;
            _localData.Game = _selectedGame;
        }
        else
        {
            GD.Print($"Join Game failed: {message}");
        }
    }

    private async void RequestServerList()
    {
        GD.Print("Request Server List");
        await _network.RequestServerList();
    }

    private void _on_refresh_button_pressed()
    {
        RequestServerList();
    }

    private void OnServerListUpdate(List<GameInfo> servers)
    {
        _serverList = servers;
        _serverBrowser.Clear();
        foreach (var game in servers)
        {
            _serverBrowser.AddItem($"{game.GameName}");
            _serverBrowser.AddItem($"{game.Players.Count}", null, false);
            _serverBrowser.AddItem($"{game.GridRow}x{game.GridColumn}", null, false);
            _serverBrowser.AddItem($"{game.State}", null, false);
            _serverBrowser.AddItem($"{game.Creator.Name}", null, false);
        }
    }

    public void _on_server_list_item_selected(int index)
    {
        GD.Print($"Selected Index : {index}");
        _selectedGame = _serverList[index / 5];
        GD.Print($"Game selected : {_selectedGame.GameId}, {_selectedGame.GameName}");
        Button connectButton = GetNode<Button>("%ButtonConnect");
        connectButton.Disabled = false;
    }
    
    private void OnConnectionStatusChanged(bool isConnected, string message)
    {
        if (!isConnected)
        {
            GD.Print($"Connection error: {message}");
            GetTree().ChangeSceneToFile("res://Scenes/Login.tscn");
        }
    }

    public override void _ExitTree()
    {
        _network.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _network.OnServerListUpdate -= OnServerListUpdate;
        _network.OnCreateGameResult -= OnCreateGameResult;
        _network.JoinGameResult -= OnJoinGameResult;
    }
}
