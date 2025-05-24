#nullable enable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class MinterInstance
    {
        [Key]
        public long MinterInstanceEntityId { get; set; } // Auto-incrementing PK for this table

        [Required]
        public long PlayerMemeMintPlayerDataId { get; set; } // FK to PlayerMemeMintPlayerData
        public virtual PlayerMemeMintPlayerData PlayerMemeMintPlayerData { get; set; } = null!;

        [Required]
        public int ClientInstanceId { get; set; } // The 1, 2, 3... ID the client uses for the slot

        [Required]
        public MinterState State { get; set; } = MinterState.Idle;

        public float TimeRemainingSeconds { get; set; } = 0f;

        public bool IsUnlocked { get; set; } = false; // Default to false, first one set true by logic

        public DateTime? LastCycleStartTimeUTC { get; set; } // When the current/last cycle started
                                                            // Nullable if idle and never run
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 
