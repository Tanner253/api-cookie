#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerMemeMintPlayerData
    {
        [Key]
        [ForeignKey("Player")] // Links to Player.PlayerId
        public long PlayerId { get; set; }
        public virtual Player Player { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(28, 8)")] // Using the precision from AppDbContext for consistency
        public decimal PlayerGCMPMPoints { get; set; } = 0M;

        [Required]
        public int SharedMintProgress { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for the minter instances owned by this player's meme mint data
        public virtual ICollection<MinterInstance> MinterInstances { get; set; } =
            new List<MinterInstance>();
    }
}
