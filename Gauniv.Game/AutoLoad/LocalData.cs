using System.Collections.Generic;
using Godot;

public partial class LocalData : Node
{
    public static LocalData Instance;

    public GameInfo Game = new();
    public Player Player = new();
    public List<GameResult> Result = new();

    public override void _Ready()
    {
        Instance = this;
    }
}