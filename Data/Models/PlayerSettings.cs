using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerSettings
    {
        [Key, ForeignKey("Player")]
        public long PlayerId { get; set; }

        public double MusicVolume { get; set; } = 1.0;

        public double SfxVolume { get; set; } = 1.0;

        public bool NotificationsEnabled { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Player? Player { get; set; }
    }
} 