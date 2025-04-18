using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class AgeVerificationStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Status is UK, configure in DbContext
        public string Status { get; set; } = string.Empty; // e.g., "NotVerified", "Pending", "Verified", "Failed"

        public string? Description { get; set; }

        public DateTime DateModified { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<PlayerAgeVerification> PlayerAgeVerifications { get; set; } = new List<PlayerAgeVerification>();
    }
} 