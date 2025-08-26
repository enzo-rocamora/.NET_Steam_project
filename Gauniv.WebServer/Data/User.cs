using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gauniv.WebServer.Data
{
    public enum UserRole
    {
        Player,
        Admin
    }

    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string? ProfilePicturePath { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Offline;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Relations
        public virtual IEnumerable<Game> OwnedGames { get; set; } = new List<Game>();
        public virtual IEnumerable<User> Friends { get; set; } = new List<User>();
        public virtual IEnumerable<GameStatistic> Statistics { get; set; } = new List<GameStatistic>();
    }
}