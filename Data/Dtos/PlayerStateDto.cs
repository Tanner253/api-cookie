namespace Api.Data.Dtos
{
    // --- DTOs for Game State ---

    public class PlayerStateDto
    {
        // Mirror fields from Models.PlayerState that the client needs
        // Changed from long to string
        public string CurrentScore { get; set; } = "0";
        // Changed from long to string
        public string TotalLifeTimeScoreEarned { get; set; } = "0"; 
        // Changed from long to string
        public string GoldBars { get; set; } = "0";

        // Kept as long
        public long PrestigeCount { get; set; }

        public DateTime LastSaveTimestamp { get; set; }
        public double StoredOfflineTimeSeconds { get; set; } 
        public long MaxOfflineStorageHours { get; set; }
        public double TimePerClickSecond { get; set; }
    }

    // ... other DTOs ...
} 