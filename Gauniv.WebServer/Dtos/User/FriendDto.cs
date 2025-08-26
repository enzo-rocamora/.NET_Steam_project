using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Dtos.Users
{
    public class FriendDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public CurrentGameInfo? CurrentGame { get; set; }  // Null si pas en jeu
    }

    public class CurrentGameInfo
    {
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
    }
}