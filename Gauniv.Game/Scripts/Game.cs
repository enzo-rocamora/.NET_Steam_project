using System;
using System.Collections.Generic;
using Godot;

public partial class Game : Node2D
{
    private NetworkManager _network;
    private LocalData _localData;

    private PackedScene _gameResultPopupScene;

    private Label _serverName;
    private Label _gameMaster;
    private Label _playersCount;
    private ItemList _playerList;
    private ProgressBar _progressBar;
    private Label _timerLabel;
    private Timer _countDown;

    private ColorPickerButton _colorPicker;
    private bool boardCreation = true;
    private GridData gridData;

    private MarginContainer _marginContainer;

    private Button _startButton;

    private GridContainer _grid;
    private Control _leftPanel;
    private Control _rightPanel;
    private Panel[,] _cells;
    private int _currentSize;

    private bool _isPlayerFinished = false;
    private bool _isGameMasterFinished = false;

    private int GridWidth { set; get; } = 1;
    private int GridHeight { set; get; } = 1;

    // Constants for UI layout
    private const int UI_PANEL_WIDTH = 396; // Width of side panels in pixels
    private const int MIN_CELL_SIZE = 40;   // Minimum size of each cell

    private (int, int) SelectedCell { set; get; } = (-1, -1);
    private bool _isGameMaster = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = NetworkManager.Instance;
        _localData = LocalData.Instance;
        _isGameMaster = _localData.Game.GameMaster.Id == _localData.Player.Id;

        _network.ConnectionStatusChanged += OnConnectionStatusChanged;
        _network.OnGameMasterCellSelectionResponse += OnGameMasterCellSelection;
        _network.OnFinishedGameResponse += OnFinishedGameResponse;
        _network.OnGameMasterBoardResponse += OnGameMasterBoardResponse;

        _serverName = GetNode<Label>("%ServerName");
        _gameMaster = GetNode<Label>("%GameMaster");
        _playersCount = GetNode<Label>("%PlayerNumber");
        _playerList = GetNode<ItemList>("%PlayerList");
        _progressBar = GetNode<ProgressBar>("%ProgressBar");
        _countDown = GetNode<Timer>("%CountDownTimer");

        _colorPicker = GetNode<ColorPickerButton>("%ColorPicker");
        _marginContainer = GetNode<MarginContainer>("%PickColorContainer");

        _startButton = GetNode<Button>("%StartButton");

        _serverName.Text = "Server : " + _localData.Game.GameName;
        _gameMaster.Text = "GameMaster : " + _localData.Game.GameMaster.Name;
        _playersCount.Text = "Players : " + _localData.Game.Players.Count;

        _playerList.Clear();
        int playerIndex = 1;
        foreach (var player in _localData.Game.Players)
        {
            _playerList.AddItem($"{playerIndex}. {player.Value.Name}");
            playerIndex++;
        }

        _grid = GetNode<GridContainer>("%GridContainer");
        _timerLabel = GetNode<Label>("%LabelTimer");

        SetupGrid(_localData.Game.GridColumn, _localData.Game.GridRow);

        if (_isGameMaster)
        {
            _timerLabel.Visible = false;
            _marginContainer.Visible = true;
        }
        else
        {
            _startButton.Visible = false;
            _marginContainer.Visible = false;
            TimerManager.RegisterTimerLabel(_timerLabel);
        }
    }
    
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (GridWidth == 0 || GridHeight == 0)
            return;

        if (_grid.GetChildCount() == 0)
        {
            SetupGrid(GridWidth, GridHeight);
        }

        if (!_isGameMaster)
        {
            TimerManager.Update(delta);
        }

        _progressBar.Value = _countDown.TimeLeft;
    }

    public void SetupGrid(int width, int height)
    {
        // Clear existing grid
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        _grid.Columns = width;
        _cells = new Panel[width, height];
        gridData = new GridData(width, height);

        // Calculate cell size based on available space
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var availableWidth = viewportSize.X - (2 * UI_PANEL_WIDTH) - 80;
        var availableHeight = viewportSize.Y - 40;

        var cellSize = Mathf.Max(MIN_CELL_SIZE, Mathf.Min(
            Mathf.FloorToInt(availableWidth / width),
            Mathf.FloorToInt(availableHeight / height)
        ));

        GD.Print(availableHeight);

        // Create cells
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var color = (x + y) % 2 == 0 ? Colors.White : Colors.LightGray;
                var cell = CreateCell(x, y, cellSize, color);
                _grid.AddChild(cell);
                _cells[x, y] = cell;
                gridData.AddCell(x, y, color.ToHtml());
            }
        }
    }

    private Panel CreateCell(int x, int y, int size, Color color)
    {
        var cell = new Panel
        {
            CustomMinimumSize = new Vector2(size, size)
        };

        // Create a style for the cell
        var styleBox = new StyleBoxFlat
        {
            BgColor = color,
            BorderWidthBottom = 1,
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderColor = Colors.Black
        };

        cell.AddThemeStyleboxOverride("panel", styleBox);
        cell.GuiInput += (InputEvent @event) => OnCellInput(@event, x, y);

        return cell;
    }

    private void OnGameMasterBoardResponse(Guid gameId, GridData gameGrid)
    {
        // Clear existing grid
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        _grid.Columns = gameGrid.Width;
        _cells = new Panel[gameGrid.Width, gameGrid.Height];
        gridData = gameGrid;

        // Calculate cell size based on available space
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var availableWidth = viewportSize.X - (2 * UI_PANEL_WIDTH) - 80;
        var availableHeight = viewportSize.Y - 40;

        var cellSize = Mathf.Max(MIN_CELL_SIZE, Mathf.Min(
            Mathf.FloorToInt(availableWidth / gameGrid.Width),
            Mathf.FloorToInt(availableHeight / gameGrid.Height)
        ));

        // Create cells
        for (int y = 0; y < gameGrid.Height; y++)
        {
            for (int x = 0; x < gameGrid.Width; x++)
            {
                var color = new Color(gameGrid.Cells[y * gameGrid.Width + x].Color);
                var cell = CreateCell(x, y, cellSize, color);
                _grid.AddChild(cell);
                _cells[x, y] = cell;
            }
        }
    }
    
    private void OnCellInput(InputEvent @event, int x, int y)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                if (_isGameMasterFinished)
                {
                    return;
                }

                (int, int) clickedCell = (x, y);

                if (!_isGameMaster)
                {
                    if (SelectedCell == clickedCell)
                    {
                        if (!_isPlayerFinished)
                        {
                            GD.Print($"Player clicked at {x},{y}");
                            PlayerCellSelectionResponse();
                            _isPlayerFinished = true;
                        }
                    }
                }
                else
                {
                    if (SelectedCell == clickedCell)
                    {
                        // If clicking the selected cell, deselect it
                        HighlightSelectedCell(x, y, false);
                    }
                    else if (SelectedCell == (-1, -1))
                    {
                        // If no cell is selected, select this one
                        HighlightSelectedCell(x, y, true);
                    }
                    else
                    {
                        // If another cell is selected, deselect it and select the new one
                        HighlightSelectedCell(SelectedCell.Item1, SelectedCell.Item2, false);
                        HighlightSelectedCell(x, y, true);
                    }
                }
            }
        }
    }

    public async void PlayerCellSelectionResponse()
    {
        _countDown.Stop();
        TimerManager.Stop();
        TimerManager.UnregisterTimerLabel(_timerLabel);
        await _network.PlayerCellSelectionResponse(_localData.Game.GameId, _localData.Player.Id, TimerManager.GetElapsedTime());
        OpenGameResultPopup();
    }

    public void HighlightCell(int x, int y, Color color)
    {
        if (x < 0 || x >= _currentSize || y < 0 || y >= _currentSize)
            return;

        var cell = _cells[x, y];
        var style = (StyleBoxFlat)cell.GetThemeStylebox("panel");
        style.BgColor = color;
        cell.AddThemeStyleboxOverride("panel", style);
    }

    private void HighlightSelectedCell(int x, int y, bool selected)
    {
        if (_cells[x, y] == null) return;

        var cell = _cells[x, y];
        var style = (StyleBoxFlat)cell.GetThemeStylebox("panel");

        if (!boardCreation)
        {
            if (selected)
            {
                style.BgColor = Colors.Yellow;  // Or any color you prefer for selection
                SelectedCell = (x, y);
                _startButton.Disabled = false;
            }
            else
            {
                // Reset to original checkerboard pattern
                style.BgColor = (x + y) % 2 == 0 ? Colors.White : Colors.LightGray;
                SelectedCell = (-1, -1);
                _startButton.Disabled = true;
            }
        }
        else
        {
            style.BgColor = _colorPicker.Color;
            gridData.ChangeCell(x, y, _colorPicker.Color.ToHtml());
        }

        cell.AddThemeStyleboxOverride("panel", style);
    }

    public async void _on_button_start_pressed()
    {
        if (!boardCreation)
        {
            GD.Print("Sending Selected Cell");
            _startButton.Disabled = true;
            await _network.GameMasterCellSelection(_localData.Game.GameId, _localData.Player.Id, SelectedCell);
            _countDown.Start(15);
            _isGameMasterFinished = true;
        }
        else 
        {
            GD.Print("Sending Game Board");
            boardCreation = false;
            _startButton.Text = "Send Cell !";
            _startButton.Disabled = true;
            await _network.GameMasterBoardSelection(_localData.Game.GameId, _localData.Player.Id, gridData);
        }
    }

    private void OnGameMasterCellSelection(Guid gameId, (int, int) selectedCell)
    {
        SelectedCell = selectedCell;
        var cell = _cells[SelectedCell.Item1, SelectedCell.Item2];
        var style = (StyleBoxFlat)cell.GetThemeStylebox("panel");
        style.BgColor = Colors.Yellow;
        TimerManager.Start();
        _countDown.Start(15);
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
        _localData.Result = gameResult;
        OpenGameResultPopup();
    }

    public void OnCountDownTimerTimeout()
    {
        GD.Print("Countdown Timer Timeout, too slow !");
        OpenGameResultPopup();
        if (!_isPlayerFinished)
        {   
            if (!_isGameMaster)
            {
                PlayerCellSelectionResponse();
            }
        }
    }

    public void OpenGameResultPopup()
    {
        if (_gameResultPopupScene is null)
        {
            _gameResultPopupScene = GD.Load<PackedScene>("res://Scenes/GameResultPopup.tscn");
            var gameResultPopup = _gameResultPopupScene.Instantiate();
            AddChild(gameResultPopup);
        }
    }

    public override void _ExitTree()
    {
        _network.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _network.OnGameMasterCellSelectionResponse -= OnGameMasterCellSelection;
        _network.OnFinishedGameResponse -= OnFinishedGameResponse;
        _network.OnGameMasterBoardResponse -= OnGameMasterBoardResponse;
    }
}
