using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class Leaderboard
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Name is UK, configure in DbContext
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? SortOrder { get; set; } = "DESC"; // e.g., "ASC", "DESC"

        public string? ResetFrequency { get; set; } // e.g., "Daily", "Weekly", "Monthly", "Never"

        // Navigation property
        public virtual ICollection<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();
    }
} 