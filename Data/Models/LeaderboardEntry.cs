using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    // Composite key will be defined in DbContext using Fluent API
    public class LeaderboardEntry
    {
        [ForeignKey("Leaderboard")]
        public long LeaderboardId { get; set; }

        [ForeignKey("Player")]
        public long PlayerId { get; set; }

        public long Score { get; set; }

        public int Rank { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Leaderboard? Leaderboard { get; set; }
        public virtual Player? Player { get; set; }
    }
} 