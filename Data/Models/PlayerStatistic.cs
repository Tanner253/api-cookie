using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerStatistic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long StatisticId { get; set; }

        [ForeignKey("Player")]
        public long PlayerId { get; set; }

        [ForeignKey("Statistic")] // Assuming FK relation to Statistic based on ERD lines
        public long StatisticDefinitionId { get; set; } // Renamed to avoid clash with PK

        public string? Name { get; set; } // Consider if this should come from Statistic entity

        public double NumericValue { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Player? Player { get; set; }
        public virtual Statistic? Statistic { get; set; }
    }
} 