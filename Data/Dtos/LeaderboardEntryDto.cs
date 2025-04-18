using System;

namespace Api.Data.Dtos
{
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TotalLifetimeScore { get; set; } = "0"; // Keep as string to match PlayerState
        public long PrestigeCount { get; set; }
    }
} 