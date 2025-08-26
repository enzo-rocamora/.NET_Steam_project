namespace Gauniv.WebServer.Dtos.Games
{
    public class GameQueryParameters
    {
        public int? Offset { get; set; }
        public int? Limit { get; set; } = 10;
        public int[]? Categories { get; set; }
    }
}