using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class PlayerFriendStatus
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        // Name is UK, configure in DbContext
        public string Name { get; set; } = string.Empty; // e.g., "Requested", "Accepted", "Blocked"

        // Navigation property
        public virtual ICollection<PlayerFriend> PlayerFriends { get; set; } = new List<PlayerFriend>();
    }
} 