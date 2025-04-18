using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class ChatMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ChatMessageID { get; set; }

        [ForeignKey("Player")]
        public long PlayerId { get; set; }

        public string MessageContent { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Player? Player { get; set; }
    }
} 