using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerChatInfo
    {
        [Key, ForeignKey("Player")]
        public long PlayerId { get; set; }

        public string? ChatUsername { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsAgeVerified { get; set; }

        // Navigation property
        public virtual Player? Player { get; set; }
    }
} 