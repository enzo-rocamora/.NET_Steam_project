using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<GameStatistic> GameStatistics { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure many-to-many relationship between Games and Categories
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Categories)
                .WithMany(c => c.Games);

            // Configure many-to-many relationship between Users and Games (ownership)
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Owners)
                .WithMany(u => u.OwnedGames);

            // Configure many-to-many self-referencing relationship for User friends
            modelBuilder.Entity<User>()
                .HasMany(u => u.Friends)
                .WithMany();

            // Configure one-to-many relationship between Game/User and Statistics
            modelBuilder.Entity<GameStatistic>()
                .HasOne(gs => gs.Game)
                .WithMany()
                .HasForeignKey(gs => gs.GameId);

            modelBuilder.Entity<GameStatistic>()
                .HasOne(gs => gs.User)
                .WithMany(u => u.Statistics)
                .HasForeignKey(gs => gs.UserId);
        }
    }
}