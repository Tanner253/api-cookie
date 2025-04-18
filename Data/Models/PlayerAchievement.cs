using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    // Composite key will be defined in DbContext using Fluent API
    public class PlayerAchievement
    {
        [ForeignKey("Player")]
        public long PlayerId { get; set; }

        [ForeignKey("Achievement")]
        public long AchievementId { get; set; }

        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        public bool RewardClaimed { get; set; }

        // Navigation properties
        public virtual Player? Player { get; set; }
        public virtual Achievement? Achievement { get; set; }
    }
} 