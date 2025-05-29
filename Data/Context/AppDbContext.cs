using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Api.Data.Models;

namespace Api.Data.Context
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
              
        }

        // New DbSets for all the models based on the ERD
        public DbSet<Player> Players { get; set; } = null!;
        public DbSet<PlayerChatInfo> PlayerChatInfos { get; set; } = null!;
        public DbSet<PlayerSettings> PlayerSettings { get; set; } = null!;
        public DbSet<PlayerState> PlayerStates { get; set; } = null!;
        public DbSet<PlayerStatistic> PlayerStatistics { get; set; } = null!;
        public DbSet<Statistic> Statistics { get; set; } = null!;
        public DbSet<PlayerAchievement> PlayerAchievements { get; set; } = null!;
        public DbSet<Achievement> Achievements { get; set; } = null!;
        public DbSet<PlayerUpgrade> PlayerUpgrades { get; set; } = null!;
        public DbSet<Upgrade> Upgrades { get; set; } = null!;
        public DbSet<UpgradeType> UpgradeTypes { get; set; } = null!;
        public DbSet<PlayerDailyReward> PlayerDailyRewards { get; set; } = null!;
        public DbSet<DailyReward> DailyRewards { get; set; } = null!;
        public DbSet<MutedPlayer> MutedPlayers { get; set; } = null!;
        public DbSet<PlayerFriend> PlayerFriends { get; set; } = null!;
        public DbSet<PlayerFriendStatus> PlayerFriendStatuses { get; set; } = null!;
        public DbSet<Leaderboard> Leaderboards { get; set; } = null!;
        public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; } = null!;
        public DbSet<PlayerAgeVerification> PlayerAgeVerifications { get; set; } = null!;
        public DbSet<AgeVerificationStatus> AgeVerificationStatuses { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

        // ADD DbSet for AdMob SSV Transactions , still working on getting valid callback even tho it passed the callback verification on the unit itself... curious 
        public DbSet<AdMobSsvTransaction> AdMobSsvTransactions { get; set; } = null!;

        // ADD NEW DBSETS for Meme Mint Feature
        public DbSet<PlayerMemeMintPlayerData> PlayerMemeMintDatas { get; set; } = null!;
        public DbSet<MinterInstance> MinterInstances { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call the base method first

            // --- Unique Constraints ---
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.FirebaseUid)
                .IsUnique();

            // Add unique index for DeviceId
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.DeviceId)
                .IsUnique();

            modelBuilder.Entity<Statistic>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<Achievement>()
                .HasIndex(a => a.Name)
                .IsUnique();

            modelBuilder.Entity<Upgrade>()
                .HasIndex(u => u.Name)
                .IsUnique();

            modelBuilder.Entity<UpgradeType>()
                .HasIndex(ut => ut.Name)
                .IsUnique();

            modelBuilder.Entity<DailyReward>()
                .HasIndex(dr => dr.DayNumber)
                .IsUnique();
            // Ensure Id and DayNumber are the same if Id is PK
            modelBuilder.Entity<DailyReward>()
                .Property(dr => dr.Id)
                .ValueGeneratedNever(); // PK is not database generated
             modelBuilder.Entity<DailyReward>()
                 .HasAlternateKey(dr => dr.DayNumber); // Treat DayNumber as unique key


            modelBuilder.Entity<PlayerFriendStatus>()
                .HasIndex(pfs => pfs.Name)
                .IsUnique();

            modelBuilder.Entity<Leaderboard>()
                .HasIndex(l => l.Name)
                .IsUnique();

            modelBuilder.Entity<AgeVerificationStatus>()
                .HasIndex(avs => avs.Status)
                .IsUnique();

            // --- Composite Keys ---
            modelBuilder.Entity<PlayerAchievement>()
                .HasKey(pa => new { pa.PlayerId, pa.AchievementId });

            modelBuilder.Entity<PlayerUpgrade>()
                .HasKey(pu => new { pu.PlayerId, pu.UpgradeId });

            modelBuilder.Entity<PlayerDailyReward>()
                .HasKey(pdr => new { pdr.PlayerId, pdr.DailyRewardId });

            modelBuilder.Entity<MutedPlayer>()
                .HasKey(mp => new { mp.MuterPlayerId, mp.MutedPlayerId });

            modelBuilder.Entity<PlayerFriend>()
                .HasKey(pf => new { pf.PlayerId, pf.FriendPlayerId });

            modelBuilder.Entity<LeaderboardEntry>()
                .HasKey(le => new { le.LeaderboardId, le.PlayerId });

            // --- Relationships ---

            // Player 1-to-1 relationships
            modelBuilder.Entity<Player>()
                .HasOne(p => p.PlayerChatInfo)
                .WithOne(pci => pci.Player)
                .HasForeignKey<PlayerChatInfo>(pci => pci.PlayerId);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.PlayerSettings)
                .WithOne(ps => ps.Player)
                .HasForeignKey<PlayerSettings>(ps => ps.PlayerId);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.PlayerState)
                .WithOne(pst => pst.Player)
                .HasForeignKey<PlayerState>(pst => pst.PlayerId);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.PlayerAgeVerification)
                .WithOne(pav => pav.Player)
                .HasForeignKey<PlayerAgeVerification>(pav => pav.PlayerId);

            // Player 1-to-Many relationships (already implicitly handled by FKs/Navigations, but can be explicit)
            modelBuilder.Entity<Player>()
                .HasMany(p => p.PlayerStatistics)
                .WithOne(ps => ps.Player)
                .HasForeignKey(ps => ps.PlayerId);

            modelBuilder.Entity<Player>()
               .HasMany(p => p.ChatMessages)
               .WithOne(cm => cm.Player)
               .HasForeignKey(cm => cm.PlayerId);

            // Statistic 1-to-Many
            modelBuilder.Entity<Statistic>()
                .HasMany(s => s.PlayerStatistics)
                .WithOne(ps => ps.Statistic)
                .HasForeignKey(ps => ps.StatisticDefinitionId); // Corrected FK name


            // Many-to-Many: Player <-> Achievement (via PlayerAchievement)
            modelBuilder.Entity<PlayerAchievement>()
                .HasOne(pa => pa.Player)
                .WithMany(p => p.PlayerAchievements)
                .HasForeignKey(pa => pa.PlayerId);
            modelBuilder.Entity<PlayerAchievement>()
                .HasOne(pa => pa.Achievement)
                .WithMany(a => a.PlayerAchievements)
                .HasForeignKey(pa => pa.AchievementId);

            // Many-to-Many: Player <-> Upgrade (via PlayerUpgrade)
            modelBuilder.Entity<PlayerUpgrade>()
                .HasOne(pu => pu.Player)
                .WithMany(p => p.PlayerUpgrades)
                .HasForeignKey(pu => pu.PlayerId);
            modelBuilder.Entity<PlayerUpgrade>()
                .HasOne(pu => pu.Upgrade)
                .WithMany(u => u.PlayerUpgrades)
                .HasForeignKey(pu => pu.UpgradeId);

            // UpgradeType 1-to-Many Upgrade
             modelBuilder.Entity<UpgradeType>()
                .HasMany(ut => ut.Upgrades)
                .WithOne(u => u.UpgradeType)
                .HasForeignKey(u => u.UpgradeTypeId);


            // Many-to-Many: Player <-> DailyReward (via PlayerDailyReward)
            modelBuilder.Entity<PlayerDailyReward>()
                .HasOne(pdr => pdr.Player)
                .WithMany(p => p.PlayerDailyRewards)
                .HasForeignKey(pdr => pdr.PlayerId);
            modelBuilder.Entity<PlayerDailyReward>()
                .HasOne(pdr => pdr.DailyReward)
                .WithMany(dr => dr.PlayerDailyRewards)
                .HasForeignKey(pdr => pdr.DailyRewardId);

            // Many-to-Many: Player <-> Player (Muting) via MutedPlayer
            modelBuilder.Entity<MutedPlayer>()
                .HasOne(mp => mp.MuterPlayer)
                .WithMany(p => p.MutedByPlayers) // Players muted BY this player
                .HasForeignKey(mp => mp.MuterPlayerId)
                .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete issues

            modelBuilder.Entity<MutedPlayer>()
                .HasOne(mp => mp.MutedPlayerRelation)
                .WithMany(p => p.MutingPlayers) // Players WHO muted this player
                .HasForeignKey(mp => mp.MutedPlayerId)
                .OnDelete(DeleteBehavior.Restrict); // Avoid cascade delete issues

            // Many-to-Many: Player <-> Player (Friends) via PlayerFriend
            modelBuilder.Entity<PlayerFriend>()
                .HasOne(pf => pf.Player)
                .WithMany(p => p.Friends) // Friends initiated by this player
                .HasForeignKey(pf => pf.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayerFriend>()
                .HasOne(pf => pf.FriendPlayer)
                .WithMany(p => p.FriendOf) // Friendships where this player is the friend
                .HasForeignKey(pf => pf.FriendPlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            // PlayerFriendStatus 1-to-Many PlayerFriend
            modelBuilder.Entity<PlayerFriendStatus>()
                .HasMany(pfs => pfs.PlayerFriends)
                .WithOne(pf => pf.PlayerFriendStatus)
                .HasForeignKey(pf => pf.PlayerFriendStatusId);

            // Many-to-Many: Leaderboard <-> Player (via LeaderboardEntry)
            modelBuilder.Entity<LeaderboardEntry>()
                .HasOne(le => le.Leaderboard)
                .WithMany(l => l.LeaderboardEntries)
                .HasForeignKey(le => le.LeaderboardId);
            modelBuilder.Entity<LeaderboardEntry>()
                .HasOne(le => le.Player)
                .WithMany(p => p.LeaderboardEntries)
                .HasForeignKey(le => le.PlayerId);


            // AgeVerificationStatus 1-to-Many PlayerAgeVerification
            modelBuilder.Entity<AgeVerificationStatus>()
                .HasMany(avs => avs.PlayerAgeVerifications)
                .WithOne(pav => pav.AgeVerificationStatus)
                .HasForeignKey(pav => pav.AgeVerificationStatusId);


            // --- Column Type Configuration ---
            // Map string properties to jsonb for PostgreSQL
             modelBuilder.Entity<Achievement>()
                 .Property(a => a.ConditionJson)
                 .HasColumnType("jsonb");

             modelBuilder.Entity<Upgrade>()
                 .Property(u => u.UnlockRequirementsJson)
                 .HasColumnType("jsonb");

            // Configure decimal precision (example, adjust if needed)
            modelBuilder.Entity<Upgrade>()
                .Property(u => u.CostScalingFactor)
                .HasPrecision(18, 4); // Example: 18 total digits, 4 decimal places

            // --- New Configurations for Meme Mint Feature ---

            // Player one-to-one PlayerMemeMintPlayerData
            modelBuilder.Entity<Player>()
                .HasOne(p => p.MemeMintPlayerData)
                .WithOne(pmmpd => pmmpd.Player)
                .HasForeignKey<PlayerMemeMintPlayerData>(pmmpd => pmmpd.PlayerId);

            // PlayerMemeMintPlayerData one-to-many MinterInstances
            modelBuilder.Entity<PlayerMemeMintPlayerData>()
                .HasMany(pmmpd => pmmpd.MinterInstances)
                .WithOne(mi => mi.PlayerMemeMintPlayerData)
                .HasForeignKey(mi => mi.PlayerMemeMintPlayerDataId);

            // Optional: Composite index on MinterInstance if ClientInstanceId should be unique per player
            modelBuilder.Entity<MinterInstance>()
                .HasIndex(mi => new { mi.PlayerMemeMintPlayerDataId, mi.ClientInstanceId })
                .IsUnique();
            
            // Configure decimal precision for PlayerGCMPMPoints if not done via attribute
            modelBuilder.Entity<PlayerMemeMintPlayerData>()
                .Property(p => p.PlayerGCMPMPoints)
                .HasColumnType("decimal(28, 8)"); // Example: 28 total digits, 8 decimal places, adjust as needed

            // --- Seed Data --- 
            SeedStaticData(modelBuilder);

        }

        private static void SeedStaticData(ModelBuilder modelBuilder)
        {
            // Seed UpgradeTypes
            modelBuilder.Entity<UpgradeType>().HasData(
                new UpgradeType { Id = 1, Name = "Click", Description = "Upgrades affecting click power." },
                new UpgradeType { Id = 2, Name = "Production", Description = "Upgrades affecting automatic production." },
                new UpgradeType { Id = 3, Name = "Prestige", Description = "Special upgrades purchased with prestige currency." }
            );

            // Seed AgeVerificationStatuses
            var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc); // Use a fixed date
            modelBuilder.Entity<AgeVerificationStatus>().HasData(
                new AgeVerificationStatus { Id = 1, Status = "Not Verified", Description = "User has not verified their age.", DateModified = seedDate },
                new AgeVerificationStatus { Id = 2, Status = "Verified", Description = "User has verified they meet age requirements.", DateModified = seedDate },
                new AgeVerificationStatus { Id = 3, Status = "Pending", Description = "User age verification is pending.", DateModified = seedDate }
            );

            // Seed PlayerFriendStatuses (Removed Description)
            modelBuilder.Entity<PlayerFriendStatus>().HasData(
                new PlayerFriendStatus { Id = 1, Name = "Pending" },
                new PlayerFriendStatus { Id = 2, Name = "Accepted" },
                new PlayerFriendStatus { Id = 3, Name = "Declined" },
                new PlayerFriendStatus { Id = 4, Name = "Blocked" }
            );

            // Seed Statistics (Definitions) (Changed StatisticId to Id)
            modelBuilder.Entity<Statistic>().HasData(
                new Statistic { Id = 1, Name = "Total Clicks", Description = "Total number of times the player has clicked.", StatType = "Counter" },
                new Statistic { Id = 2, Name = "Total Score Earned", Description = "Total score earned across all sessions.", StatType = "Counter" },
                new Statistic { Id = 3, Name = "Total Prestige Count", Description = "Total number of times the player has prestiged.", StatType = "Counter" },
                new Statistic { Id = 4, Name = "Current Gold Bars", Description = "Current amount of prestige currency held.", StatType = "Value" }
                // Add more specific game stats as needed (e.g., MaxScore, BuildingsOwned)
            );

            // Seed Upgrades (Definitions) - Using actual game data
            // EffectScalingFactor assumed 1.0 (linear) unless logic dictates otherwise
            // MaxLevel assumed 0 (infinite) unless logic dictates otherwise
            modelBuilder.Entity<Upgrade>().HasData(
                // --- Click Upgrades (TypeId = 1) ---
                new Upgrade { Id = 1, Name = "Basic Click Upgrade", Description = "Adds +0.1 score per click per level.", 
                              BaseCost = 10L, CostScalingFactor = 1.07M, BaseEffectValue = 0.1, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 1 },
                new Upgrade { Id = 2, Name = "Iron Click Upgrade", Description = "Adds +0.5 score per click per level.", 
                              BaseCost = 25L, CostScalingFactor = 1.08M, BaseEffectValue = 0.5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 1 },
                new Upgrade { Id = 3, Name = "Copper Click Upgrade", Description = "Adds +1.5 score per click per level.", 
                              BaseCost = 75L, CostScalingFactor = 1.1M, BaseEffectValue = 1.5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 1 },
                new Upgrade { Id = 4, Name = "Silver Click Upgrade", Description = "Adds +2 score per click per level.", 
                              BaseCost = 100L, CostScalingFactor = 1.12M, BaseEffectValue = 2.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 1 },
                new Upgrade { Id = 5, Name = "Gold Click Upgrade", Description = "Adds +5 score per click per level.", 
                              BaseCost = 250L, CostScalingFactor = 1.14M, BaseEffectValue = 5.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 1 },
                new Upgrade { Id = 6, Name = "Diamond Click Upgrade", Description = "Adds +10 score per click per level.", 
                              BaseCost = 500L, CostScalingFactor = 1.2M, BaseEffectValue = 10.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 1 },

                // --- Production Upgrades (TypeId = 2) --- Note: BaseEffectValue = Score/Second per level
                new Upgrade { Id = 101, Name = "Auto-Clicker", Description = "Generates +1.0 score/sec per level.", 
                              BaseCost = 10L, CostScalingFactor = 1.15M, BaseEffectValue = 1.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 102, Name = "Click Farm", Description = "Generates +3.33 score/sec per level.", 
                              BaseCost = 50L, CostScalingFactor = 1.2M, BaseEffectValue = 5.0/1.5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 103, Name = "Click Factory", Description = "Generates +7.5 score/sec per level.", 
                              BaseCost = 200L, CostScalingFactor = 1.25M, BaseEffectValue = 15.0/2.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 104, Name = "Click MegaCorp", Description = "Generates +16.0 score/sec per level.", 
                              BaseCost = 600L, CostScalingFactor = 1.3M, BaseEffectValue = 40.0/2.5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 105, Name = "Click Enterprise", Description = "Generates +33.33 score/sec per level.", 
                              BaseCost = 1500L, CostScalingFactor = 1.35M, BaseEffectValue = 100.0/3.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 106, Name = "Click Conglomerate", Description = "Generates +71.43 score/sec per level.", 
                              BaseCost = 5000L, CostScalingFactor = 1.4M, BaseEffectValue = 250.0/3.5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 107, Name = "Click Syndicate", Description = "Generates +150.0 score/sec per level.", 
                              BaseCost = 10000L, CostScalingFactor = 1.45M, BaseEffectValue = 600.0/4.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 108, Name = "Click Collective", Description = "Generates +333.33 score/sec per level.", 
                              BaseCost = 25000L, CostScalingFactor = 1.5M, BaseEffectValue = 1500.0/4.5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 109, Name = "Click Singularity", Description = "Generates +800.0 score/sec per level.", 
                              BaseCost = 60000L, CostScalingFactor = 1.55M, BaseEffectValue = 4000.0/5.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },
                new Upgrade { Id = 110, Name = "Click Deity", Description = "Generates +1666.67 score/sec per level.", 
                              BaseCost = 150000L, CostScalingFactor = 1.6M, BaseEffectValue = 10000.0/6.0, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 2 },

                // --- Prestige Upgrades (TypeId = 3) --- Cost is Gold Bars
                new Upgrade { Id = 201, Name = "Lemon", Description = "Catch lemons to gain 10 minutes worth of production, instantly!", 
                              BaseCost = 1000L, CostScalingFactor = 2.0M, BaseEffectValue = 0, EffectScalingFactor = 1.0, MaxLevel = 1, IsUnique = true, UpgradeTypeId = 3 }, // Unlock Feature
                new Upgrade { Id = 202, Name = "Lemon Lifespan", Description = "Lemons don't spoil as fast (+2s per level).", 
                              BaseCost = 1000L, CostScalingFactor = 1.5M, BaseEffectValue = 2, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 3 },
                new Upgrade { Id = 203, Name = "Lemon Spawn Rate", Description = "Lemons spawn faster (-5s avg time per level?).", // Description needs clarification based on actual effect
                              BaseCost = 1000L, CostScalingFactor = 2.1M, BaseEffectValue = -5, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 3 },
                new Upgrade { Id = 204, Name = "Lemon Value", Description = "Increases the value of lemons (+1 prod minutes per level).", 
                              BaseCost = 1000L, CostScalingFactor = 2.5M, BaseEffectValue = 1, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 3 },
                new Upgrade { Id = 205, Name = "Click Multiplier", Description = "Multiply your clicks! (+10% base per level).", 
                              BaseCost = 1200L, CostScalingFactor = 4.0M, BaseEffectValue = 0.1, EffectScalingFactor = 1.0, MaxLevel = 0, IsUnique = false, UpgradeTypeId = 3 }
            );

            // Seed Achievements (Definitions)
            modelBuilder.Entity<Achievement>().HasData(
                new Achievement { Id = 1, Name = "First Click", Description = "You made your first click!", ConditionJson = "{\"stat\":\"Total Clicks\",\"op\":\">=\",\"value\":1}", IconAssetName = "ach_click1", RewardType = "None", RewardAmount = 0 },
                new Achievement { Id = 2, Name = "Score!", Description = "Reached 1000 score.", ConditionJson = "{\"stat\":\"Total Score Earned\",\"op\":\">=\",\"value\":1000}", IconAssetName = "ach_score1", RewardType = "GoldBars", RewardAmount = 1 },
                new Achievement { Id = 3, Name = "Producer", Description = "Bought your first production building.", ConditionJson = "{\"upgradeTypeLevel\":\"Production\",\"op\":\">=\",\"value\":1}", IconAssetName = "ach_prod1", RewardType = "None", RewardAmount = 0 },
                 new Achievement { Id = 4, Name = "Prestigious", Description = "Prestiged for the first time.", ConditionJson = "{\"stat\":\"Total Prestige Count\",\"op\":\">=\",\"value\":1}", IconAssetName = "ach_prestige1", RewardType = "GoldBars", RewardAmount = 5 }
            );

            // Seed DailyRewards (Definitions)
            modelBuilder.Entity<DailyReward>().HasData(
                new DailyReward { Id = 1, DayNumber = 1, RewardType = "Score", RewardAmount = 100, Description = "Day 1 Reward!" },
                new DailyReward { Id = 2, DayNumber = 2, RewardType = "Score", RewardAmount = 500, Description = "Day 2 Reward!" },
                new DailyReward { Id = 3, DayNumber = 3, RewardType = "GoldBars", RewardAmount = 1, Description = "Day 3 Reward!" },
                new DailyReward { Id = 4, DayNumber = 4, RewardType = "Score", RewardAmount = 2500, Description = "Day 4 Reward!" },
                new DailyReward { Id = 5, DayNumber = 5, RewardType = "Score", RewardAmount = 10000, Description = "Day 5 Reward!" },
                new DailyReward { Id = 6, DayNumber = 6, RewardType = "GoldBars", RewardAmount = 3, Description = "Day 6 Reward!" },
                new DailyReward { Id = 7, DayNumber = 7, RewardType = "GoldBars", RewardAmount = 5, Description = "Day 7 Reward! HUGE BONUS!" }
                // Add more days if the cycle is longer
            );
             // Ensure DayNumber is treated as the key for finding rewards (as Id isn't auto-generated)
             // This was already configured earlier, but reaffirming its importance.

            // Seed Leaderboards (Definitions)
            modelBuilder.Entity<Leaderboard>().HasData(
                 new Leaderboard { Id = 1, Name = "Total Score", Description = "All-time highest score achieved.", SortOrder = "DESC", ResetFrequency = "Never" },
                 new Leaderboard { Id = 2, Name = "Clicks This Week", Description = "Most clicks in the current week.", SortOrder = "DESC", ResetFrequency = "Weekly" },
                 new Leaderboard { Id = 3, Name = "Prestige Count", Description = "Highest prestige level reached.", SortOrder = "DESC", ResetFrequency = "Never" }
             );
        }

    }
}