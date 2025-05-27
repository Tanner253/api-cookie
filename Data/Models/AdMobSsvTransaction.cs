using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class AdMobSsvTransaction
    {
        [Key]
        [StringLength(255)] // Max length for typical transaction IDs
        public required string TransactionId { get; set; }

        public long? PlayerId { get; set; } // Made PlayerId nullable

        [ForeignKey("PlayerId")]
        public Player? Player { get; set; } // Navigation property

        [StringLength(100)]
        public required string RewardItem { get; set; }

        public decimal RewardAmount { get; set; } // Using decimal to match PlayerState.GoldBars potentially

        public DateTime AdCompletionTimestamp { get; set; } // Timestamp from AdMob callback

        public DateTime ProcessedAt { get; set; } // Timestamp when your server processed it
    }
} 