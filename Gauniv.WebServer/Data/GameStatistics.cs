using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Gauniv.WebServer.Data
{
    public class GameStatistic
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameId { get; set; }
        public virtual Game Game { get; set; } = null!;

        [Required]
        public string UserId { get; set; } = string.Empty; 
        public virtual User User { get; set; } = null!;

        public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan TotalPlayTime { get; set; }
        public int TimesPlayed { get; set; }
    }
}