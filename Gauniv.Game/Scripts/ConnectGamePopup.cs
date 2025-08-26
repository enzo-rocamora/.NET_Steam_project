using System;
using System.Collections.Generic;
using Godot;

public partial class ConnectGamePopup : Control
{
    private NetworkManager _network;
    private LocalData _localData;

    private HBoxContainer _playerContainer;
    private Label _gameNameContainer;
    private Button _readyButton;
    private Button _startButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = NetworkManager.Instance;
        _localData = LocalData.Instance;

        _network.GameMasterDisconnected += OnGameMasterDisconnected;
        _network.OnPlayerListUpdate += OnPlayerListUpdate;
        _network.OnStartGameResult += OnStartGameResult;

        _gameNameContainer = GetNode<Label>("%GameNameLabel");
        _playerContainer = GetNode<HBoxContainer>("%PlayerContainer");

        _readyButton = GetNode<Button>("%ReadyButton");
        _startButton = GetNode<Button>("%StartButton");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public async void _on_start_button_pressed()
    {
        await _network.StartGameRequest(_localData.Game.GameId, _localData.Player.Id);
    }

    private void OnPlayerListUpdate(Dictionary<Guid, Player> Players)
    {
        GD.Print($"Updating Player list : {Players.Count} players found");
        _localData.Game.Players = Players;
        _gameNameContainer.Text = "Game : " + _localData.Game.GameName;

        // Clear existing player containers
        foreach (var child in _playerContainer.GetChildren())
        {
            child.Free();
        }

        // Create a container for each player
        bool allPlayersReady = true;
        foreach (var player in _localData.Game.Players.Values)
        {
            var playerScene = ResourceLoader.Load<PackedScene>("res://Scenes/Player.tscn");
            var playerInstance = playerScene.Instantiate<MarginContainer>();

            var playerNameLabel = playerInstance.GetNode<Label>("%PlayerNameLabel");
            playerNameLabel.Text = player.Name;
            playerNameLabel.Modulate = player.Ready ? Colors.Green : Colors.Red;

            _playerContainer.AddChild(playerInstance);

            if (!player.Ready)
            {
                allPlayersReady = false;
            }
        }

        if (_localData.Player.Id == _localData.Game.Creator.Id)
        {
            _startButton.Visible = true;
            _readyButton.Visible = false;
        }
        else
        {
            _startButton.Visible = false;
            _readyButton.Visible = true;
        }

        _startButton.Disabled = _localData.Game.Players.Count <= 1 || !allPlayersReady;
    }

    public async void _on_ready_button_pressed()
    {
        await _network.PlayerReadyRequest(_localData.Game.GameId, _localData.Player.Id);
        _readyButton.Disabled = true;
    }

    private void OnStartGameResult(bool success, GameInfo game, string message)
    {
        if (success)
        {
            _localData.Game = game;
            GetTree().ChangeSceneToFile("res://Scenes/Game.tscn");
        }
        else
        {
            GD.Print($"Failed to start game: {message}");
        }
    }

    private void OnGameMasterDisconnected()
    {
        GD.Print("Game Exited!");
        GetParent().RemoveChild(this);
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
        _network.OnPlayerListUpdate -= OnPlayerListUpdate;
        _network.OnStartGameResult -= OnStartGameResult;
        _network.GameMasterDisconnected -= OnGameMasterDisconnected;
    }
}
