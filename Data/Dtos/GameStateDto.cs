#nullable enable // Enable nullable context for this file
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Api.Data.Dtos
{
    // NOTE: Removed duplicate PlayerDto definition from here. It now resides in Data/Dtos/PlayerDto.cs

    // --- DTOs for Game State ---

    public class PlayerSettingsDto
    {
        // Mirror fields from Models.PlayerSettings
        public double MusicVolume { get; set; }
        public double SfxVolume { get; set; }
        public bool NotificationsEnabled { get; set; }
    }

    public class PlayerUpgradeDto
    {
        // Identify the upgrade and its level
        public long UpgradeId { get; set; }
        public int Level { get; set; }
    }

    public class PlayerAchievementDto
    {
        // Identify the achievement and when it was unlocked
        public long AchievementId { get; set; }
        public DateTime UnlockedAt { get; set; }
    }

    public class PlayerStatisticDto
    {
        // Identify the statistic and its value
        public long StatisticDefinitionId { get; set; } // Corresponds to Statistic.StatisticId
        public double NumericValue { get; set; }
        public string? StringValue { get; set; } // Add if statistics can have string values
    }

    public class PlayerChatInfoDto
    {
        public string? ChatUsername { get; set; }
    }

    public class PlayerAgeVerificationDto
    {
        public bool IsVerified { get; set; }
        // Optional: Include status ID/text or verification timestamp if needed by client
        // public long AgeVerificationStatusId { get; set; }
        // public DateTime? VerifiedAtTimestamp { get; set; }
    }

    // --- DTO for Updating Username --- 

    public class UpdateUsernameRequestDto
    {
        [Required]
        [StringLength(20, MinimumLength = 1)] // Match potential validation
        public string? ChatUsername { get; set; }
    }

    // --- Main Game State DTO ---

    public class GameStateDto
    {
        [Required]
        public PlayerStateDto PlayerState { get; set; } = new PlayerStateDto();

        [Required]
        public PlayerSettingsDto PlayerSettings { get; set; } = new PlayerSettingsDto();

        // Add Chat Info and Age Verification DTOs
        [Required]
        public PlayerChatInfoDto PlayerChatInfo { get; set; } = new PlayerChatInfoDto();

        [Required]
        public PlayerAgeVerificationDto PlayerAgeVerification { get; set; } = new PlayerAgeVerificationDto();

        [Required]
        public List<PlayerUpgradeDto> PlayerUpgrades { get; set; } = new List<PlayerUpgradeDto>();

        [Required]
        public List<PlayerAchievementDto> PlayerAchievements { get; set; } = new List<PlayerAchievementDto>();

        [Required]
        public List<PlayerStatisticDto> PlayerStatistics { get; set; } = new List<PlayerStatisticDto>();

        // ADDED MemeMintData
        public MemeMintPlayerDataDto? MemeMintData { get; set; } // Initialized to null, will be populated if player has data

        // Muted players might be handled by a separate endpoint/DTO due to complexity
        // public List<long> MutedPlayerIds { get; set; } = new List<long>(); 
    }
} 