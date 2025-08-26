using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos.Users;

namespace Gauniv.WebServer.Services
{
    public class UserApiService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UserApiService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<string>> GetUserFriendsIdsAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.Id == userId);

            return user?.Friends.Select(f => f.Id) ?? Enumerable.Empty<string>();
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.Friends)
                .Include(u => u.Statistics)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task UpdateUserStatusAsync(string userId, UserStatus status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            user.Status = status;
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserStatusOfflineAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Statistics)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            // Si l'utilisateur était en jeu, mettre à jour les statistiques
            if (user.Status == UserStatus.InGame)
            {
                var lastGameStat = user.Statistics
                    .OrderByDescending(s => s.LastPlayedAt)
                    .FirstOrDefault();

                if (lastGameStat != null)
                {
                    // Calculer le temps de jeu
                    var playTime = DateTime.UtcNow - lastGameStat.LastPlayedAt;
                    lastGameStat.TotalPlayTime += playTime;
                }
            }

            user.Status = UserStatus.Offline;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserGameStatusAsync(string userId, int? gameId)
        {
            var user = await _context.Users
                .Include(u => u.Statistics)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (gameId.HasValue)
            {
                // Vérifier si le jeu existe
                var game = await _context.Games.FindAsync(gameId.Value);
                if (game == null)
                    throw new InvalidOperationException("Game not found");

                user.Status = UserStatus.InGame;

                // Créer ou mettre à jour les statistiques
                var stats = user.Statistics.FirstOrDefault(s => s.GameId == gameId.Value);
                if (stats == null)
                {
                    stats = new GameStatistic
                    {
                        GameId = gameId.Value,
                        UserId = userId,
                        LastPlayedAt = DateTime.UtcNow,
                        TimesPlayed = 1,
                        TotalPlayTime = TimeSpan.Zero
                    };
                    _context.GameStatistics.Add(stats);
                }
                else
                {
                    stats.LastPlayedAt = DateTime.UtcNow;
                    stats.TimesPlayed++;
                }
            }
            else
            {
                // Si l'utilisateur était en jeu, mettre à jour les statistiques
                if (user.Status == UserStatus.InGame)
                {
                    var lastGameStat = user.Statistics
                        .OrderByDescending(s => s.LastPlayedAt)
                        .FirstOrDefault();

                    if (lastGameStat != null)
                    {
                        var playTime = DateTime.UtcNow - lastGameStat.LastPlayedAt;
                        lastGameStat.TotalPlayTime += playTime;
                    }
                }

                user.Status = UserStatus.Online;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<FriendDto>> GetUserFriendsAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Friends)
                .ThenInclude(f => f.Statistics)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            return _mapper.Map<IEnumerable<FriendDto>>(user.Friends);
        }

        public async Task<UserStatus> GetCurrentStatusAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            return user.Status;
        }

        public async Task AddFriendAsync(string userId, FriendRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            // Recherche de l'ami selon les critères fournis
            var friendQuery = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Id))
            {
                friendQuery = friendQuery.Where(u => u.Id == request.Id);
            }
            else if (!string.IsNullOrWhiteSpace(request.Email))
            {
                friendQuery = friendQuery.Where(u => u.Email == request.Email);
            }
            else if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                friendQuery = friendQuery.Where(u => u.UserName == request.UserName);
            }

            var friend = await friendQuery.FirstOrDefaultAsync();

            if (friend == null)
                throw new InvalidOperationException("Friend not found");

            if (friend.Id == userId)
                throw new InvalidOperationException("Cannot add yourself as a friend");

            if (user.Friends.Any(f => f.Id == friend.Id))
                throw new InvalidOperationException("User is already your friend");

            user.Friends = user.Friends.Append(friend).ToList();
            await _context.SaveChangesAsync();
        }
        public async Task<object> GetUserStatusAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.Statistics)
                    .ThenInclude(s => s.Game)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            var currentGame = user.Status == UserStatus.InGame
                ? user.Statistics
                    .OrderByDescending(s => s.LastPlayedAt)
                    .FirstOrDefault()
                : null;

            return new
            {
                status = user.Status,
                lastSeen = user.LastLoginAt,
                currentGame = currentGame != null ? new
                {
                    gameId = currentGame.GameId,
                    title = currentGame.Game.Title,
                    startedAt = currentGame.LastPlayedAt
                } : null
            };
        }

        public async Task<IEnumerable<UserGameDto>> GetUserGamesAsync(string userId)
        {
            var user = await _context.Users
                .Include(u => u.OwnedGames)
                    .ThenInclude(g => g.Categories)
                .Include(u => u.Statistics)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            return user.OwnedGames.Select(game =>
            {
                var stats = user.Statistics.FirstOrDefault(s => s.GameId == game.Id);
                return new UserGameDto
                {
                    Id = game.Id,
                    Title = game.Title,
                    Categories = game.Categories.Select(c => c.Name),
                    TotalPlayTime = stats?.TotalPlayTime,
                    LastPlayedAt = stats?.LastPlayedAt,
                    PurchaseDate = DateTime.UtcNow // À remplacer par la vraie date d'achat si vous l'ajoutez au modèle
                };
            });
        }

        public async Task PurchaseGameAsync(string userId, int gameId)
        {
            var user = await _context.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var game = await _context.Games.FindAsync(gameId);

            if (user == null)
                throw new InvalidOperationException("User not found");

            if (game == null)
                throw new InvalidOperationException("Game not found");

            if (user.OwnedGames.Any(g => g.Id == gameId))
                throw new InvalidOperationException("User already owns this game");

            user.OwnedGames = user.OwnedGames.Append(game).ToList();
            await _context.SaveChangesAsync();
        }


    }
}