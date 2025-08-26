using Gauniv.WebServer.Data;
using Gauniv.WebServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Services
{
    public class SetupService : IHostedService
    {
        private ApplicationDbContext? applicationDbContext;
        private readonly IServiceProvider serviceProvider;
        private Task? task;

        public SetupService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                if (applicationDbContext is null)
                {
                    throw new Exception("ApplicationDbContext is null");
                }

                // Appliquer les migrations en attente
                if (applicationDbContext.Database.GetPendingMigrations().Any())
                {
                    await applicationDbContext.Database.MigrateAsync(cancellationToken);
                }

                // Récupérer les services Identity
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

                // Créer le rôle Admin s'il n'existe pas
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // Créer l'administrateur par défaut
                if (!await userManager.Users.AnyAsync(u => u.Email == "admin@gauniv.com", cancellationToken))
                {
                    var admin = new User
                    {
                        UserName = "admin@gauniv.com",
                        Email = "admin@gauniv.com",
                        FirstName = "Admin",
                        LastName = "User",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(admin, "Admin123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new Exception($"Failed to create admin user: {errors}");
                    }
                }

                List<Category> categories = await applicationDbContext.Categories.ToListAsync(cancellationToken: cancellationToken);

                // Créer des catégories par défaut
                if (categories.Count < 4)
                {
                    var new_categories = new[]
                    {
                        new Category { Name = "Action" },
                        new Category { Name = "Adventure" },
                        new Category { Name = "RPG" },
                        new Category { Name = "Simulation" },
                        new Category { Name = "Strategy" },
                        new Category { Name = "Sports" },
                        new Category { Name = "Puzzle" },
                        new Category { Name = "Casual" },
                        new Category { Name = "Indie" }
                    };
                    await applicationDbContext.Categories.AddRangeAsync(new_categories, cancellationToken);
                    await applicationDbContext.SaveChangesAsync(cancellationToken);

                    // Recharger les catégories après l'ajout
                    categories = await applicationDbContext.Categories.ToListAsync(cancellationToken);
                }

                // Créer les jeux par défaut s'il n'y en a pas
                if (!await applicationDbContext.Games.AnyAsync(cancellationToken: cancellationToken))
                {
                    var games = new[]
                    {
                        new Game
                        {
                            Title = "The Witcher 3: Wild Hunt",
                            Description = "The Witcher 3: Wild Hunt is a story-driven, next-generation open world role-playing game set in a visually stunning fantasy universe full of meaningful choices and impactful consequences. In The Witcher, you play as professional monster hunter Geralt of Rivia tasked with finding a child of prophecy in a vast open world rich with merchant cities, pirate islands, dangerous mountain passes, and forgotten caverns to explore.",
                            CreatedAt = DateTime.SpecifyKind(new DateTime(2015, 5, 19), DateTimeKind.Utc),
                            Price = 29.99m,
                            Categories = new List<Category> { categories.First(c => c.Name == "RPG") }
                        },
                        new Game
                        {
                            Title = "Grand Theft Auto V",
                            Description = "When a young street hustler, a retired bank robber and a terrifying psychopath find themselves entangled with some of the most frightening and deranged elements of the criminal underworld, the U.S. government and the entertainment industry, they must pull off a series of dangerous heists to survive in a ruthless city in which they can trust nobody, least of all each other.",
                            CreatedAt = DateTime.SpecifyKind(new DateTime(2015, 4, 14), DateTimeKind.Utc),
                            Categories = new List<Category> { categories.First(c => c.Name == "Action") },
                            Price = 19.99m
                        },
                        new Game
                        {
                            Title = "Red Dead Redemption 2",
                            Description = "America, 1899. The end of the wild west era has begun as lawmen hunt down the last remaining outlaw gangs. Those who will not surrender or succumb are killed. After a robbery goes badly wrong in the western town of Blackwater, Arthur Morgan and the Van der Linde gang are forced to flee. With federal agents and the best bounty hunters in the nation massing on their heels, the gang must rob, steal and fight their way across the rugged heartland of America in order to survive. As deepening internal divisions threaten to tear the gang apart, Arthur must make a choice between his own ideals and loyalty to the gang who raised him.",
                            CreatedAt = DateTime.SpecifyKind(new DateTime(2019, 11, 5), DateTimeKind.Utc),
                            Categories = new List<Category> { categories.First(c => c.Name == "Action") },
                            Price = 39.99m
                        },
                        new Game
                        {
                            Title = "Cyberpunk 2077",
                            Description = "Cyberpunk 2077 is an open-world, action-adventure story set in Night City, a megalopolis obsessed with power, glamour and body modification. You play as V, a mercenary outlaw going after a one-of-a-kind implant that is the key to immortality. You can customize your character's cyberware, skillset and playstyle, and explore a vast city where the choices you make shape the story and the world around you.",
                            CreatedAt = DateTime.SpecifyKind(new DateTime(2020, 12, 10), DateTimeKind.Utc),
                            Categories = new List<Category> {
                                categories.First(c => c.Name == "Action"),
                                categories.First(c => c.Name == "Adventure")
                            },
                            Price = 49.99m
                        }
                    };

                    // Ajouter les jeux à la base de données
                    await applicationDbContext.Games.AddRangeAsync(games, cancellationToken);
                    await applicationDbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task UpdateUserStatusAsync(string userId, UserStatus status)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = status;
                user.LastLoginAt = DateTime.UtcNow;  // Déjà en UTC
                await context.SaveChangesAsync();
            }
        }

        public static async Task UpdateUserStatusAsync(ApplicationDbContext context, string userId, UserStatus status)
        {
            var user = await context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = status;
                if (status == UserStatus.Online)
                {
                    user.LastLoginAt = DateTime.UtcNow;  // Déjà en UTC
                }
                await context.SaveChangesAsync();
            }
        }
    }
}