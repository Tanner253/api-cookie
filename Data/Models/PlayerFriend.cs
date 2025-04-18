using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    // Composite key will be defined in DbContext using Fluent API
    public class PlayerFriend
    {
        [ForeignKey("Player")]
        public long PlayerId { get; set; }

        [ForeignKey("FriendPlayer")]
        public long FriendPlayerId { get; set; }

        public DateTime FriendshipDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("PlayerFriendStatus")]
        public long PlayerFriendStatusId { get; set; }

        // Navigation properties
        public virtual Player? Player { get; set; }
        public virtual Player? FriendPlayer { get; set; }
        public virtual PlayerFriendStatus? PlayerFriendStatus { get; set; }
    }
} 