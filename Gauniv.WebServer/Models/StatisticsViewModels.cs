namespace Gauniv.WebServer.Models
{
    public class GlobalStatisticsViewModel
    {
        public int TotalGames { get; set; }
        public List<CategoryStatistics> CategoryStatistics { get; set; } = new();
        public int CurrentOnlinePlayers { get; set; }
        public int CurrentInGamePlayers { get; set; }
        public TimeSpan TotalPlayTime { get; set; }

        public string FormattedTotalPlayTime =>
            $"{Math.Floor(TotalPlayTime.TotalHours):N0}h {TotalPlayTime.Minutes:D2}m";
    }

    public class CategoryStatistics
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public long GamesCount { get; set; }
        public long TotalPurchases { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public string FormattedTotalPlayTime =>
    $"{Math.Floor(TotalPlayTime.TotalHours):N0}h {TotalPlayTime.Minutes:D2}m";
    }

    public class GameStatisticsViewModel
    {
        public int GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public int TotalPurchases { get; set; }
        public int CurrentPlayers { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public TimeSpan AveragePlayTime { get; set; }
        public List<string> Categories { get; set; } = new();

        public string FormattedTotalPlayTime =>
            $"{Math.Floor(TotalPlayTime.TotalHours):N0}h {TotalPlayTime.Minutes:D2}m";

        public string FormattedAveragePlayTime =>
            $"{Math.Floor(AveragePlayTime.TotalHours):N0}h {AveragePlayTime.Minutes:D2}m";
    }

    public class CategoryStatisticsViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public int TotalPurchases { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public TimeSpan AveragePlayTimePerPurchase { get; set; }
        public List<GameStatistics> GameStatistics { get; set; } = new();

        public string FormattedTotalPlayTime =>
            $"{Math.Floor(TotalPlayTime.TotalHours):00}h {TotalPlayTime.Minutes:00}m";

        public string FormattedAveragePlayTime =>
            $"{Math.Floor(AveragePlayTimePerPurchase.TotalHours):00}h {AveragePlayTimePerPurchase.Minutes:00}m";
    }


    public class GameStatistics
    {
        public int GameId { get; set; }
        public string GameTitle { get; set; } = string.Empty;
        public int Purchases { get; set; }
        public TimeSpan PlayTime { get; set; }
        public TimeSpan AveragePlayTime { get; set; }

        public string FormattedPlayTime =>
            $"{Math.Floor(PlayTime.TotalHours):00}h {PlayTime.Minutes:00}m";

        public string FormattedAveragePlayTime =>
            $"{Math.Floor(AveragePlayTime.TotalHours):00}h {AveragePlayTime.Minutes:00}m";
    }

}