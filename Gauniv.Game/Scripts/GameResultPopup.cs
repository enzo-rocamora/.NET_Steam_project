using System.Collections.Generic;
using System;
using System.Linq;
using Godot;

public partial class GameResultPopup : Control
{
    private NetworkManager _network;
    private LocalData _localData;

    private Button _returnButton;

    private ItemList _results;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = NetworkManager.Instance;
        _localData = LocalData.Instance;

        _network.ConnectionStatusChanged += OnConnectionStatusChanged;
        _network.OnFinishedGameResponse += OnFinishedGameResponse;

        _results = GetNode<ItemList>("%PlayerResults");

        _returnButton = GetNode<Button>("%ReturnButton");

        if (_localData.Result.Count != 0)
        {
            _results.Clear();

            var filteredResults = _localData.Result.Where(r => r.Position != -1).OrderBy(r => r.Position);
            foreach (var result in filteredResults)
            {
                _results.AddItem($"{result.Position}. {result.PlayerName}");
            }

            //filteredResults = _localData.Result.Where(r => r.Position == -1).OrderBy(r => r.Position);
            //foreach (var result in filteredResults)
            //{
            //    _results2.AddItem($"{result.PlayerName}");
            //}

            if (filteredResults.Count() == 0)
            {
                _results.AddItem("No Eligible Player !");
            }

            _returnButton.Disabled = false;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void OnReturnButtonPressed()
    {
        _localData.Result = new();
        _localData.Game = new();
        _localData.Player.Ready = false;
        _localData.Player.Position = 0;
        GetTree().ChangeSceneToFile("res://Scenes/ServerBrowser.tscn");
    }

    private void OnConnectionStatusChanged(bool isConnected, string message)
    {
        if (!isConnected)
        {
            GD.Print($"Connection error: {message}");
            GetTree().ChangeSceneToFile("res://Scenes/Login.tscn");
        }
    }

    public void OnFinishedGameResponse(Guid gameId, List<GameResult> gameResult)
    {
        GD.Print($"Game Result Event Triggered ! {gameResult.Count}");
        _results = GetNode<ItemList>("%PlayerResults");
        _results.Clear();
        
        var positionCounter = 1;
        var filteredResults = _localData.Result.Where(r => r.Position != -1).OrderBy(r => r.Position);
        foreach (var result in filteredResults)
        {
            _results.AddItem($"{positionCounter}. {result.PlayerName}");
            positionCounter++;
        }

        if (filteredResults.Count() == 0)
        {
            _results.AddItem("No Eligible Player !");
        }

        _returnButton = GetNode<Button>("%ReturnButton");
        _returnButton.Disabled = false;
    }

    public override void _ExitTree()
    {
        _network.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _network.OnFinishedGameResponse -= OnFinishedGameResponse;
    }
}
