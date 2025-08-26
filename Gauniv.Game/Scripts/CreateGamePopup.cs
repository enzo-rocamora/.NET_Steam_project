using Godot;

public partial class CreateGamePopup : Control
{
    private NetworkManager _network;
    private LocalData _localData;

    private PackedScene _connectGamePopupScene;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _network = NetworkManager.Instance;
        _localData = LocalData.Instance;

        _network.ConnectionStatusChanged += OnConnectionStatusChanged;
        //_network.CreateGameResult += OnCreateGameResult;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey eventKey)
            if (eventKey.Pressed && eventKey.Keycode == Key.Escape)
                GetParent().RemoveChild(this);
    }

    public async void _on_button_generate_pressed()
    {
        GD.Print("Creating Game...");

        LineEdit nameBox = GetNode<LineEdit>("%NameBox");

        string name = nameBox.Text;

        SpinBox widthBox = GetNode<SpinBox>("%HeightBox");
        SpinBox heightBox = GetNode<SpinBox>("%WidthBox");

        int width = (int)widthBox.Value;
        int height = (int)heightBox.Value;

        GD.Print("Request Server List");
        await _network.CreateNewGameRequest(name, height, width);
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
        _network.ConnectionStatusChanged -= OnConnectionStatusChanged;
    }
}
