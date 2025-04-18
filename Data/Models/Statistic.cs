using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class Statistic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Name is UK, configure in DbContext
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? StatType { get; set; } // Consider Enum if predefined types

        // Navigation property
        public virtual ICollection<PlayerStatistic> PlayerStatistics { get; set; } = new List<PlayerStatistic>();
    }
} 