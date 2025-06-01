using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerAgeVerification
    {
        [Key, ForeignKey("Player")]
        public long PlayerId { get; set; }

        [ForeignKey("AgeVerificationStatus")]
        public long AgeVerificationStatusId { get; set; }

        public DateTime? VerifiedAt { get; set; }

        public string? VerificationMethod { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public DateTime? LastVerificationAttempt { get; set; }

        public int VerificationAttemptCount { get; set; }

        // Navigation properties
        public virtual Player? Player { get; set; }
        public virtual AgeVerificationStatus? AgeVerificationStatus { get; set; }
    }
} 