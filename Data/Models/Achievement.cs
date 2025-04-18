using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class Achievement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Name is UK, configure in DbContext
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Condition { get; set; } // Textual description of condition

        // This will be mapped to jsonb in DbContext
        public string? ConditionJson { get; set; } // JSON structure for programmatic evaluation

        public string? IconAssetName { get; set; }

        public string? RewardType { get; set; } // Consider Enum

        public long RewardAmount { get; set; }

        // Navigation property
        public virtual ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
    }
} 