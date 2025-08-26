using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Models
{
    public class OnlineUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public UserStatus Status { get; set; }
        public int? CurrentGameId { get; set; }
        public string? gameName { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime LastLoginAt { get; set; }

        public string StatusClass => Status switch
        {
            UserStatus.Online => "bg-success",
            UserStatus.InGame => "bg-primary",
            _ => "bg-secondary"
        };

        public string StatusText => Status switch
        {
            UserStatus.Online => "Online",
            UserStatus.InGame => "In Game",
            _ => "Offline"
        };
    }
}