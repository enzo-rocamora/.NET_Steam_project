using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.Data;

namespace Gauniv.WebServer.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllUsersWithGamesAsync()
        {
            return await _context.Users
                .Include(u => u.OwnedGames)
                .OrderByDescending(u => u.LastLoginAt)
                .ToListAsync();
        }

        public async Task<User?> GetUserDetailsAsync(string id)
        {
            return await _context.Users
                .Include(u => u.OwnedGames)
                    .ThenInclude(g => g.Categories)
                .Include(u => u.Statistics)
                    .ThenInclude(s => s.Game)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public string GetTotalPlayTime(User user)
        {
            var totalMinutes = user.Statistics.Sum(s => s.TotalPlayTime.TotalMinutes);
            var hours = Math.Floor(totalMinutes / 60);
            var minutes = totalMinutes % 60;
            return $"{hours}h {minutes}m";
        }

        public Game? GetMostPlayedGame(User user)
        {
            return user.Statistics
                .OrderByDescending(s => s.TotalPlayTime)
                .FirstOrDefault()?.Game;
        }

        public DateTime? GetLastPlayTime(User user)
        {
            return user.Statistics
                .OrderByDescending(s => s.LastPlayedAt)
                .FirstOrDefault()?.LastPlayedAt;
        }
    }
}