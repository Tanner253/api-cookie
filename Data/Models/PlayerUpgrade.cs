using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    // Composite key will be defined in DbContext using Fluent API
    public class PlayerUpgrade
    {
        [ForeignKey("Player")]
        public long PlayerId { get; set; }

        [ForeignKey("Upgrade")]
        public long UpgradeId { get; set; }

        public int Level { get; set; }

        public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLeveledAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Player? Player { get; set; }
        public virtual Upgrade? Upgrade { get; set; }
    }
} 