using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Data.Models
{
    public class Player
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long PlayerId { get; set; }

        // FirebaseUid is marked as UK in the ERD, will configure uniqueness in DbContext
        public string FirebaseUid { get; set; } = string.Empty;

        public string? ChatDeviceId { get; set; }

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PlayerChatInfo? PlayerChatInfo { get; set; }
        public virtual PlayerSettings? PlayerSettings { get; set; }
        public virtual PlayerState? PlayerState { get; set; }
        public virtual ICollection<PlayerStatistic> PlayerStatistics { get; set; } = new List<PlayerStatistic>();
        public virtual ICollection<PlayerAchievement> PlayerAchievements { get; set; } = new List<PlayerAchievement>();
        public virtual ICollection<PlayerUpgrade> PlayerUpgrades { get; set; } = new List<PlayerUpgrade>();
        public virtual ICollection<PlayerDailyReward> PlayerDailyRewards { get; set; } = new List<PlayerDailyReward>();
        public virtual ICollection<MutedPlayer> MutedByPlayers { get; set; } = new List<MutedPlayer>(); // Players muted by this player
        public virtual ICollection<MutedPlayer> MutingPlayers { get; set; } = new List<MutedPlayer>(); // Players who muted this player
        public virtual ICollection<PlayerFriend> Friends { get; set; } = new List<PlayerFriend>(); // Friendships initiated by this player
        public virtual ICollection<PlayerFriend> FriendOf { get; set; } = new List<PlayerFriend>(); // Friendships where this player is the friend
        public virtual ICollection<LeaderboardEntry> LeaderboardEntries { get; set; } = new List<LeaderboardEntry>();
        public virtual PlayerAgeVerification? PlayerAgeVerification { get; set; }
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
} 