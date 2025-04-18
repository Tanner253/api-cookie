using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerState
    {
        [Key, ForeignKey("Player")]
        public long PlayerId { get; set; }

        public string CurrentScore { get; set; } = "0";

        public string TotalLifeTimeScoreEarned { get; set; } = "0";

        public string GoldBars { get; set; } = "0";

        public long PrestigeCount { get; set; } = 0;

        public DateTime LastSaveTimestamp { get; set; } = DateTime.UtcNow;

        public double StoredOfflineTimeSeconds { get; set; } = 0;

        public long MaxOfflineStorageHours { get; set; } = 2; // Example default

        public double TimePerClickSecond { get; set; } = 0; // Or appropriate default

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Player? Player { get; set; }
    }
} 