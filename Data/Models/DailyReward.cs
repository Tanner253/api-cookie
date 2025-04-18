using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class DailyReward
    {
        [Key]
        // Assuming DayNumber is the PK, not auto-generated
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; } // Corresponds to DayNumber

        // DayNumber is UK and PK, configure in DbContext
        public int DayNumber { get; set; }

        public string? RewardType { get; set; } // Consider Enum

        public long RewardAmount { get; set; }

        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<PlayerDailyReward> PlayerDailyRewards { get; set; } = new List<PlayerDailyReward>();
    }
} 