using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using MessagePack;

[Union(0, typeof(AuthenticationRequest))]
[Union(1, typeof(AuthenticationResponse))]
[Union(2, typeof(GameInfo))]
[Union(3, typeof(ServerListRequest))]
[Union(4, typeof(ServerListResponse))]
[Union(5, typeof(CreateNewGameRequest))]
[Union(6, typeof(CreateNewGameResponse))]
[Union(7, typeof(JoinGameRequest))]
[Union(8, typeof(JoinGameResponse))]
[Union(9, typeof(PlayerListRequest))]
[Union(10, typeof(PlayerListResponse))]
[Union(11, typeof(PlayerReadyRequest))]
[Union(12, typeof(StartGameRequest))]
[Union(13, typeof(StartGameResponse))]
[Union(14, typeof(GameMasterCellSelection))]
[Union(15, typeof(GameMasterCellSelectionResponse))]
[Union(16, typeof(PlayerCellSelectionResponse))]
[Union(17, typeof(FinishedGameResponse))]
[Union(18, typeof(DisconnectPlayer))]
[Union(19, typeof(GameMasterBoardResponse))]
[Union(20, typeof(GameMasterBoardSelection))]
public interface IGameMessage
{

}

[MessagePackObject(true)]
public record AuthenticationRequest : IGameMessage
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

[MessagePackObject(true)]
public class AuthenticationResponse : IGameMessage
{
    public required bool Success { get; set; }
    public Player Player { get; set; }
    public required string Message { get; set; }
}

[MessagePackObject(true)]
public class GameInfo : IGameMessage
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public string GameName { get; set; } = "DefaultGameName";
    public int GridRow { get; set; } = 0;
    public int GridColumn { get; set; } = 0;
    public Player Creator { get; set; }
    public Player GameMaster { get; set; }
    public GameState State { get; set; } = GameState.WaitingForPlayers;
    public (int, int)? SelectedCell { get; set; }
    public Dictionary<Guid, Player> Players { get; set; } = new Dictionary<Guid, Player>();
}

public enum GameState
{
    WaitingForPlayers,
    InProgress,
    Finished
}

[MessagePackObject(true)]
public class ServerListRequest : IGameMessage
{
}

[MessagePackObject(true)]
public class ServerListResponse : IGameMessage
{
    public required List<GameInfo> Servers { get; set; }
}

[MessagePackObject(true)]
public class CreateNewGameRequest : IGameMessage
{
    public required string GameName { get; set; }

    public required int GridRow { get; set; }

    public required int GridColumn { get; set; }

}

[MessagePackObject(true)]
public class CreateNewGameResponse : IGameMessage
{
    public required bool Success { get; set; }

    public GameInfo Game { get; set; }

    public required string Message { get; set; }

}

[MessagePackObject(true)]
public class JoinGameRequest : IGameMessage
{
    public required Guid GameId { get; set; }
}

[MessagePackObject(true)]
public class JoinGameResponse : IGameMessage
{
    public required bool Success { get; set; }

    public required string Message { get; set; }
}

[MessagePackObject(true)]
public class PlayerListRequest : IGameMessage
{
    public required Guid GameId { get; set; }
}

[MessagePackObject(true)]
public class PlayerListResponse : IGameMessage
{
    public required Dictionary<Guid, Player> Players { get; set; }
}

[MessagePackObject(true)]
public class Player : IGameMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; }
    public string Name { get; set; }
    public bool Ready { get; set; } = false;
    public int Position { get; set; }
}

[MessagePackObject(true)]
public class PlayerReadyRequest : IGameMessage
{
    public required Guid GameId { get; set; }
    public required Guid PlayerId { get; set; }
}

[MessagePackObject(true)]
public class StartGameRequest : IGameMessage
{
    public required Guid GameId { get; set; }
    public required Guid PlayerId { get; set; }
}

[MessagePackObject(true)]
public class StartGameResponse : IGameMessage
{
    public required bool Success { get; set; }
    public GameInfo Game { get; set; }
    public required string Message { get; set; }
}

[MessagePackObject(true)]
public class GameMasterCellSelection : IGameMessage
{
    public required Guid GameId { get; set; }
    public required Guid PlayerId { get; set; }
    public required (int, int) SelectedCell { get; set; }
}

[MessagePackObject(true)]
public class GameMasterCellSelectionResponse : IGameMessage
{
    public required Guid GameId { get; set; }
    public required (int, int) SelectedCell { get; set; }
}

[MessagePackObject(true)]
public class PlayerCellSelectionResponse : IGameMessage
{
    public required Guid GameId { get; set; }
    public required Guid PlayerId { get; set; }
    public required Double ResponseTime { get; set; }
}

[MessagePackObject(true)]
public class FinishedGameResponse : IGameMessage
{
    public required Guid GameId { get; set; }
    public required List<GameResult> GameResult { get; set; }
}

[MessagePackObject(true)]
public class GameResult : IGameMessage
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; }
    public int Position { get; set; }
    public bool IsEligible { get; set; }
}

[MessagePackObject(true)]
public class DisconnectPlayer : IGameMessage
{
    public required Guid GameId { get; set; }
}

[MessagePackObject(true)]
public class GameMasterBoardSelection : IGameMessage
{
    public required Guid GameId { get; set; }
    public required Guid PlayerId { get; set; }
    public required GridData GameGrid { get; set; }
}

[MessagePackObject(true)]
public class GameMasterBoardResponse : IGameMessage
{
    public required Guid GameId { get; set; }
    public required GridData GameGrid { get; set; }
}

[MessagePackObject(true)]
public class GridData : IGameMessage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<CellData> Cells { get; set; }

    public GridData(int width, int height)
    {
        Width = width;
        Height = height;
        Cells = new List<CellData>();
    }

    public void AddCell(int x, int y, string color)
    {
        Cells.Add(new CellData(x, y, color));
    }

    public void ChangeCell(int x, int y, string color)
    {
        var cell = Cells.Find(c => c.X == x && c.Y == y);
        if (cell != null)
        {
            cell.Color = color;
        }
    }
}

[MessagePackObject(true)]
public class CellData : IGameMessage
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Color { get; set; }

    public CellData(int x, int y, string color)
    {
        X = x;
        Y = y;
        Color = color;
    }
}