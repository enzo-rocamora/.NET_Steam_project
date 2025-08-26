namespace Gauniv.Client.Models.Dto
{
    public class UserGameDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public string? TotalPlayTime { get; set; }
        public DateTime? LastPlayedAt { get; set; }
        public ICollection<string> Categories { get; set; } = new List<string>();
    }
}