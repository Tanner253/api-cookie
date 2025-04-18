using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class Upgrade
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Name is UK, configure in DbContext
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public long BaseCost { get; set; }

        public decimal CostScalingFactor { get; set; }

        public double BaseEffectValue { get; set; }

        public double EffectScalingFactor { get; set; }

        public int MaxLevel { get; set; }

        public bool IsUnique { get; set; }

        public string? IconAssetName { get; set; }

        // This will be mapped to jsonb in DbContext
        public string? UnlockRequirementsJson { get; set; }

        [ForeignKey("UpgradeType")]
        public long UpgradeTypeId { get; set; }

        // Navigation properties
        public virtual UpgradeType? UpgradeType { get; set; }
        public virtual ICollection<PlayerUpgrade> PlayerUpgrades { get; set; } = new List<PlayerUpgrade>();
    }
} 