using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Condition = table.Column<string>(type: "text", nullable: true),
                    ConditionJson = table.Column<string>(type: "jsonb", nullable: true),
                    IconAssetName = table.Column<string>(type: "text", nullable: true),
                    RewardType = table.Column<string>(type: "text", nullable: true),
                    RewardAmount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgeVerificationStatuses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DateModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeVerificationStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DayNumber = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<string>(type: "text", nullable: true),
                    RewardAmount = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyRewards", x => x.Id);
                    table.UniqueConstraint("AK_DailyRewards_DayNumber", x => x.DayNumber);
                });

            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<string>(type: "text", nullable: true),
                    ResetFrequency = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerFriendStatuses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFriendStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirebaseUid = table.Column<string>(type: "text", nullable: false),
                    ChatDeviceId = table.Column<string>(type: "text", nullable: true),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirebaseId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StatType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpgradeTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpgradeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    ChatMessageID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    MessageContent = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.ChatMessageID);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    LeaderboardId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<long>(type: "bigint", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => new { x.LeaderboardId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MutedPlayers",
                columns: table => new
                {
                    MuterPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    MutedPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    MutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutedPlayers", x => new { x.MuterPlayerId, x.MutedPlayerId });
                    table.ForeignKey(
                        name: "FK_MutedPlayers_Players_MutedPlayerId",
                        column: x => x.MutedPlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MutedPlayers_Players_MuterPlayerId",
                        column: x => x.MuterPlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAchievements",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    AchievementId = table.Column<long>(type: "bigint", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RewardClaimed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAchievements", x => new { x.PlayerId, x.AchievementId });
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAchievements_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAgeVerifications",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    AgeVerificationStatusId = table.Column<long>(type: "bigint", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationMethod = table.Column<string>(type: "text", nullable: true),
                    LastVerificationAttempt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationAttemptCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAgeVerifications", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerAgeVerifications_AgeVerificationStatuses_AgeVerificat~",
                        column: x => x.AgeVerificationStatusId,
                        principalTable: "AgeVerificationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAgeVerifications_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerChatInfos",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    ChatUsername = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAgeVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerChatInfos", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerChatInfos_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDailyRewards",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    DailyRewardId = table.Column<int>(type: "integer", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    MaxStreak = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDailyRewards", x => new { x.PlayerId, x.DailyRewardId });
                    table.ForeignKey(
                        name: "FK_PlayerDailyRewards_DailyRewards_DailyRewardId",
                        column: x => x.DailyRewardId,
                        principalTable: "DailyRewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDailyRewards_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerFriends",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    FriendPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    FriendshipDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerFriendStatusId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFriends", x => new { x.PlayerId, x.FriendPlayerId });
                    table.ForeignKey(
                        name: "FK_PlayerFriends_PlayerFriendStatuses_PlayerFriendStatusId",
                        column: x => x.PlayerFriendStatusId,
                        principalTable: "PlayerFriendStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerFriends_Players_FriendPlayerId",
                        column: x => x.FriendPlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerFriends_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSettings",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    MusicVolume = table.Column<double>(type: "double precision", nullable: false),
                    SfxVolume = table.Column<double>(type: "double precision", nullable: false),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSettings", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerSettings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStates",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentScore = table.Column<string>(type: "text", nullable: false),
                    TotalLifeTimeScoreEarned = table.Column<string>(type: "text", nullable: false),
                    GoldBars = table.Column<string>(type: "text", nullable: false),
                    PrestigeCount = table.Column<long>(type: "bigint", nullable: false),
                    LastSaveTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StoredOfflineTimeSeconds = table.Column<double>(type: "double precision", nullable: false),
                    MaxOfflineStorageHours = table.Column<long>(type: "bigint", nullable: false),
                    TimePerClickSecond = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStates", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerStates_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStatistics",
                columns: table => new
                {
                    StatisticId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    StatisticDefinitionId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    NumericValue = table.Column<double>(type: "double precision", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStatistics", x => x.StatisticId);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerStatistics_Statistics_StatisticDefinitionId",
                        column: x => x.StatisticDefinitionId,
                        principalTable: "Statistics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Upgrades",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    BaseCost = table.Column<long>(type: "bigint", nullable: false),
                    CostScalingFactor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    BaseEffectValue = table.Column<double>(type: "double precision", nullable: false),
                    EffectScalingFactor = table.Column<double>(type: "double precision", nullable: false),
                    MaxLevel = table.Column<int>(type: "integer", nullable: false),
                    IsUnique = table.Column<bool>(type: "boolean", nullable: false),
                    IconAssetName = table.Column<string>(type: "text", nullable: true),
                    UnlockRequirementsJson = table.Column<string>(type: "jsonb", nullable: true),
                    UpgradeTypeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Upgrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Upgrades_UpgradeTypes_UpgradeTypeId",
                        column: x => x.UpgradeTypeId,
                        principalTable: "UpgradeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerUpgrades",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    UpgradeId = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLeveledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerUpgrades", x => new { x.PlayerId, x.UpgradeId });
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerUpgrades_Upgrades_UpgradeId",
                        column: x => x.UpgradeId,
                        principalTable: "Upgrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Achievements",
                columns: new[] { "Id", "Condition", "ConditionJson", "Description", "IconAssetName", "Name", "RewardAmount", "RewardType" },
                values: new object[,]
                {
                    { 1L, null, "{\"stat\":\"Total Clicks\",\"op\":\">=\",\"value\":1}", "You made your first click!", "ach_click1", "First Click", 0L, "None" },
                    { 2L, null, "{\"stat\":\"Total Score Earned\",\"op\":\">=\",\"value\":1000}", "Reached 1000 score.", "ach_score1", "Score!", 1L, "GoldBars" },
                    { 3L, null, "{\"upgradeTypeLevel\":\"Production\",\"op\":\">=\",\"value\":1}", "Bought your first production building.", "ach_prod1", "Producer", 0L, "None" },
                    { 4L, null, "{\"stat\":\"Total Prestige Count\",\"op\":\">=\",\"value\":1}", "Prestiged for the first time.", "ach_prestige1", "Prestigious", 5L, "GoldBars" }
                });

            migrationBuilder.InsertData(
                table: "AgeVerificationStatuses",
                columns: new[] { "Id", "DateModified", "Description", "Status" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "User has not verified their age.", "Not Verified" },
                    { 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "User has verified they meet age requirements.", "Verified" },
                    { 3L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "User age verification is pending.", "Pending" }
                });

            migrationBuilder.InsertData(
                table: "DailyRewards",
                columns: new[] { "Id", "DayNumber", "Description", "RewardAmount", "RewardType" },
                values: new object[,]
                {
                    { 1, 1, "Day 1 Reward!", 100L, "Score" },
                    { 2, 2, "Day 2 Reward!", 500L, "Score" },
                    { 3, 3, "Day 3 Reward!", 1L, "GoldBars" },
                    { 4, 4, "Day 4 Reward!", 2500L, "Score" },
                    { 5, 5, "Day 5 Reward!", 10000L, "Score" },
                    { 6, 6, "Day 6 Reward!", 3L, "GoldBars" },
                    { 7, 7, "Day 7 Reward! HUGE BONUS!", 5L, "GoldBars" }
                });

            migrationBuilder.InsertData(
                table: "Leaderboards",
                columns: new[] { "Id", "Description", "Name", "ResetFrequency", "SortOrder" },
                values: new object[,]
                {
                    { 1L, "All-time highest score achieved.", "Total Score", "Never", "DESC" },
                    { 2L, "Most clicks in the current week.", "Clicks This Week", "Weekly", "DESC" },
                    { 3L, "Highest prestige level reached.", "Prestige Count", "Never", "DESC" }
                });

            migrationBuilder.InsertData(
                table: "PlayerFriendStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1L, "Pending" },
                    { 2L, "Accepted" },
                    { 3L, "Declined" },
                    { 4L, "Blocked" }
                });

            migrationBuilder.InsertData(
                table: "Statistics",
                columns: new[] { "Id", "Description", "Name", "StatType" },
                values: new object[,]
                {
                    { 1L, "Total number of times the player has clicked.", "Total Clicks", "Counter" },
                    { 2L, "Total score earned across all sessions.", "Total Score Earned", "Counter" },
                    { 3L, "Total number of times the player has prestiged.", "Total Prestige Count", "Counter" },
                    { 4L, "Current amount of prestige currency held.", "Current Gold Bars", "Value" }
                });

            migrationBuilder.InsertData(
                table: "UpgradeTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1L, "Upgrades affecting click power.", "Click" },
                    { 2L, "Upgrades affecting automatic production.", "Production" },
                    { 3L, "Special upgrades purchased with prestige currency.", "Prestige" }
                });

            migrationBuilder.InsertData(
                table: "Upgrades",
                columns: new[] { "Id", "BaseCost", "BaseEffectValue", "CostScalingFactor", "Description", "EffectScalingFactor", "IconAssetName", "IsUnique", "MaxLevel", "Name", "UnlockRequirementsJson", "UpgradeTypeId" },
                values: new object[,]
                {
                    { 1L, 10L, 0.10000000000000001, 1.07m, "Adds +0.1 score per click per level.", 1.0, null, false, 0, "Basic Click Upgrade", null, 1L },
                    { 2L, 25L, 0.5, 1.08m, "Adds +0.5 score per click per level.", 1.0, null, false, 0, "Iron Click Upgrade", null, 1L },
                    { 3L, 75L, 1.5, 1.1m, "Adds +1.5 score per click per level.", 1.0, null, false, 0, "Copper Click Upgrade", null, 1L },
                    { 4L, 100L, 2.0, 1.12m, "Adds +2 score per click per level.", 1.0, null, false, 0, "Silver Click Upgrade", null, 1L },
                    { 5L, 250L, 5.0, 1.14m, "Adds +5 score per click per level.", 1.0, null, false, 0, "Gold Click Upgrade", null, 1L },
                    { 6L, 500L, 10.0, 1.2m, "Adds +10 score per click per level.", 1.0, null, false, 0, "Diamond Click Upgrade", null, 1L },
                    { 101L, 10L, 1.0, 1.15m, "Generates +1.0 score/sec per level.", 1.0, null, false, 0, "Auto-Clicker", null, 2L },
                    { 102L, 50L, 3.3333333333333335, 1.2m, "Generates +3.33 score/sec per level.", 1.0, null, false, 0, "Click Farm", null, 2L },
                    { 103L, 200L, 7.5, 1.25m, "Generates +7.5 score/sec per level.", 1.0, null, false, 0, "Click Factory", null, 2L },
                    { 104L, 600L, 16.0, 1.3m, "Generates +16.0 score/sec per level.", 1.0, null, false, 0, "Click MegaCorp", null, 2L },
                    { 105L, 1500L, 33.333333333333336, 1.35m, "Generates +33.33 score/sec per level.", 1.0, null, false, 0, "Click Enterprise", null, 2L },
                    { 106L, 5000L, 71.428571428571431, 1.4m, "Generates +71.43 score/sec per level.", 1.0, null, false, 0, "Click Conglomerate", null, 2L },
                    { 107L, 10000L, 150.0, 1.45m, "Generates +150.0 score/sec per level.", 1.0, null, false, 0, "Click Syndicate", null, 2L },
                    { 108L, 25000L, 333.33333333333331, 1.5m, "Generates +333.33 score/sec per level.", 1.0, null, false, 0, "Click Collective", null, 2L },
                    { 109L, 60000L, 800.0, 1.55m, "Generates +800.0 score/sec per level.", 1.0, null, false, 0, "Click Singularity", null, 2L },
                    { 110L, 150000L, 1666.6666666666667, 1.6m, "Generates +1666.67 score/sec per level.", 1.0, null, false, 0, "Click Deity", null, 2L },
                    { 201L, 1000L, 0.0, 2.0m, "Catch lemons to gain 10 minutes worth of production, instantly!", 1.0, null, true, 1, "Lemon", null, 3L },
                    { 202L, 1000L, 2.0, 1.5m, "Lemons don't spoil as fast (+2s per level).", 1.0, null, false, 0, "Lemon Lifespan", null, 3L },
                    { 203L, 1000L, -5.0, 2.1m, "Lemons spawn faster (-5s avg time per level?).", 1.0, null, false, 0, "Lemon Spawn Rate", null, 3L },
                    { 204L, 1000L, 1.0, 2.5m, "Increases the value of lemons (+1 prod minutes per level).", 1.0, null, false, 0, "Lemon Value", null, 3L },
                    { 205L, 1200L, 0.10000000000000001, 4.0m, "Multiply your clicks! (+10% base per level).", 1.0, null, false, 0, "Click Multiplier", null, 3L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Name",
                table: "Achievements",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgeVerificationStatuses_Status",
                table: "AgeVerificationStatuses",
                column: "Status",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_PlayerId",
                table: "ChatMessages",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyRewards_DayNumber",
                table: "DailyRewards",
                column: "DayNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_PlayerId",
                table: "LeaderboardEntries",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_Name",
                table: "Leaderboards",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MutedPlayers_MutedPlayerId",
                table: "MutedPlayers",
                column: "MutedPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAchievements_AchievementId",
                table: "PlayerAchievements",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAgeVerifications_AgeVerificationStatusId",
                table: "PlayerAgeVerifications",
                column: "AgeVerificationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDailyRewards_DailyRewardId",
                table: "PlayerDailyRewards",
                column: "DailyRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFriends_FriendPlayerId",
                table: "PlayerFriends",
                column: "FriendPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFriends_PlayerFriendStatusId",
                table: "PlayerFriends",
                column: "PlayerFriendStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFriendStatuses_Name",
                table: "PlayerFriendStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_DeviceId",
                table: "Players",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_FirebaseUid",
                table: "Players",
                column: "FirebaseUid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_PlayerId",
                table: "PlayerStatistics",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatistics_StatisticDefinitionId",
                table: "PlayerStatistics",
                column: "StatisticDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerUpgrades_UpgradeId",
                table: "PlayerUpgrades",
                column: "UpgradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Statistics_Name",
                table: "Statistics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Upgrades_Name",
                table: "Upgrades",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Upgrades_UpgradeTypeId",
                table: "Upgrades",
                column: "UpgradeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UpgradeTypes_Name",
                table: "UpgradeTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "MutedPlayers");

            migrationBuilder.DropTable(
                name: "PlayerAchievements");

            migrationBuilder.DropTable(
                name: "PlayerAgeVerifications");

            migrationBuilder.DropTable(
                name: "PlayerChatInfos");

            migrationBuilder.DropTable(
                name: "PlayerDailyRewards");

            migrationBuilder.DropTable(
                name: "PlayerFriends");

            migrationBuilder.DropTable(
                name: "PlayerSettings");

            migrationBuilder.DropTable(
                name: "PlayerStates");

            migrationBuilder.DropTable(
                name: "PlayerStatistics");

            migrationBuilder.DropTable(
                name: "PlayerUpgrades");

            migrationBuilder.DropTable(
                name: "Leaderboards");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "AgeVerificationStatuses");

            migrationBuilder.DropTable(
                name: "DailyRewards");

            migrationBuilder.DropTable(
                name: "PlayerFriendStatuses");

            migrationBuilder.DropTable(
                name: "Statistics");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Upgrades");

            migrationBuilder.DropTable(
                name: "UpgradeTypes");
        }
    }
}
