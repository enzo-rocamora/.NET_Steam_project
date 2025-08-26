namespace Gauniv.WebServer.Dtos.Users
{
    public class UserGameDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public TimeSpan? TotalPlayTime { get; set; }
        public DateTime? LastPlayedAt { get; set; }
        public IEnumerable<string> Categories { get; set; } = new List<string>();
    }
}