using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    // Composite key will be defined in DbContext using Fluent API
    public class MutedPlayer
    {
        [ForeignKey("MuterPlayer")]
        public long MuterPlayerId { get; set; }

        [ForeignKey("MutedPlayerRelation")]
        public long MutedPlayerId { get; set; }

        public DateTime MutedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Player? MuterPlayer { get; set; }
        public virtual Player? MutedPlayerRelation { get; set; }
    }
} 