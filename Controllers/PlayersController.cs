using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data.Context;
using Api.Data.Models;
using System.Threading.Tasks; // Ensure this is present
using Api.Data.Dtos; // Add this using statement for DTOs

namespace Api.Controllers
{
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PlayersController(AppDbContext context)
        {
            _context = context;
        }

        // --- DTOs for Controller Actions ---
        public class IdentifyPlayerRequestDto
        {
            public required string DeviceId { get; set; }
            public required string FirebaseUid { get; set; }
        }

        public class UpdateUsernameRequestDto // Kept from original code
        {
            public required string ChatUsername { get; set; }
        }

        // --- Player Identification and Basic CRUD --- 

        // POST /api/players/identify
        [HttpPost("identify")]
        public async Task<ActionResult<PlayerDto>> IdentifyPlayer([FromBody] IdentifyPlayerRequestDto request)
        {
            if (string.IsNullOrEmpty(request.DeviceId) || string.IsNullOrEmpty(request.FirebaseUid))
            {
                return BadRequest("DeviceId and FirebaseUid cannot be empty.");
            }

            Player? player = null;
            bool createdNewPlayer = false;

            // 1. Try to find by FirebaseUid
            player = await _context.Players
                .FirstOrDefaultAsync(p => p.FirebaseUid == request.FirebaseUid);

            if (player != null)
            {
                // Player found by FirebaseUid. Update DeviceId if it has changed.
                if (player.DeviceId != request.DeviceId)
                {
                    player.DeviceId = request.DeviceId;
                }
            }
            else
            {
                // 2. Not found by FirebaseUid, try by DeviceId
                player = await _context.Players
                    .FirstOrDefaultAsync(p => p.DeviceId == request.DeviceId);

                if (player != null)
                {
                    // Player found by DeviceId. Link FirebaseUid to this player.
                    player.FirebaseUid = request.FirebaseUid;
                }
                else
                {
                    // 3. Not found by either ID, create a new player
                    player = new Player
                    {
                        DeviceId = request.DeviceId,
                        FirebaseUid = request.FirebaseUid,
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow,
                        // Initialize required related entities with default values
                        PlayerState = new PlayerState(),
                        PlayerSettings = new PlayerSettings(),
                        PlayerChatInfo = new PlayerChatInfo { ChatUsername = null, UpdatedAt = DateTime.UtcNow },
                        PlayerAgeVerification = new PlayerAgeVerification { AgeVerificationStatusId = 1, VerificationAttemptCount = 0 }
                    };
                    _context.Players.Add(player);
                    createdNewPlayer = true;
                }
            }

            // Update LastLoginAt for existing or newly created player
            player.LastLoginAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) // Catch potential unique constraint violations
            {
                 // Log the error (ex)
                 Console.WriteLine($"Error saving player identification: {ex.InnerException?.Message ?? ex.Message}");
                 // Check if it's a unique constraint violation (e.g., duplicate FirebaseUid or DeviceId assigned elsewhere)
                 // You might need database-specific checks here (e.g., check exception number for SQL Server or PostgreSQL)
                 // For simplicity, return a generic conflict or server error
                 return Conflict("A conflict occurred while trying to save player data. The FirebaseUid or DeviceId might already be associated with another player.");
            }

            // Map to DTO
            var playerDto = MapPlayerToDto(player);

            if (createdNewPlayer)
            {
                // Return 201 Created with the DTO
                return CreatedAtAction(nameof(GetPlayerById), new { id = player.PlayerId }, playerDto);
            }
            else
            {
                // Return 200 OK with the DTO
                return Ok(playerDto);
            }
        }

        // GET: api/players/{id}
        [HttpGet("{id:long}")]
        public async Task<ActionResult<PlayerDto>> GetPlayerById(long id)
        {
            var player = await _context.Players
                                     .AsNoTracking() // Use NoTracking for read-only operation
                                     .FirstOrDefaultAsync(p => p.PlayerId == id);

            if (player == null)
            {
                return NotFound($"Player {id} not found");
            }

            return Ok(MapPlayerToDto(player)); // Return DTO
        }

        // GET: api/players/findByFirebaseUid/{firebaseUid}
        [HttpGet("findByFirebaseUid/{firebaseUid}")]
        public async Task<ActionResult<PlayerDto>> FindByFirebaseUid(string firebaseUid)
        {
            if (string.IsNullOrEmpty(firebaseUid))
            {
                return BadRequest("Firebase UID cannot be empty.");
            }

            var player = await _context.Players
                                     .AsNoTracking()
                                     .Where(p => p.FirebaseUid == firebaseUid)
                                     .FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound($"Player not found with Firebase UID: {firebaseUid}");
            }

            return Ok(MapPlayerToDto(player)); // Return DTO
        }

        // Helper to map Player entity to PlayerDto
        private PlayerDto MapPlayerToDto(Player player)
        {
            return new PlayerDto
            {
                PlayerId = player.PlayerId,
                FirebaseUid = player.FirebaseUid,
                DeviceId = player.DeviceId,
                ChatDeviceId = player.ChatDeviceId,
                CreatedAt = player.CreatedAt,
                LastLoginAt = player.LastLoginAt
            };
        }

        // Helper method updated for Player
        private async Task<bool> PlayerExists(long id) // id is long
        {
            return await _context.Players.AnyAsync(e => e.PlayerId == id);
        }

        // --- Game State Endpoints --- 

        // GET: api/players/{playerId}/gameState
        [HttpGet("{playerId:long}/gameState")]
        public async Task<ActionResult<GameStateDto>> GetGameState(long playerId)
        {
            // Fetch the player and all related data needed for the game state
            var player = await _context.Players
                .Include(p => p.PlayerState)
                .Include(p => p.PlayerSettings)
                .Include(p => p.PlayerChatInfo) // Include Chat Info
                .Include(p => p.PlayerAgeVerification) // Include Age Verification
                    .ThenInclude(pav => pav.AgeVerificationStatus) // Include the status text if needed
                .Include(p => p.PlayerUpgrades)
                .Include(p => p.PlayerAchievements)
                .Include(p => p.PlayerStatistics)
                // Note: Not including MutedPlayers here for simplicity, handle via separate endpoint if needed
                .AsNoTracking() 
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null)
            {
                return NotFound($"Player {playerId} not found.");
            }

            // Ensure related entities are loaded (handle potential nulls if relationships aren't guaranteed)
            if (player.PlayerState == null || player.PlayerSettings == null || player.PlayerChatInfo == null || player.PlayerAgeVerification == null)
            {
                return StatusCode(500, "Player data is incomplete. Missing required state, settings, chat info, or age verification.");
            }

            // Map the entity data to the DTO
            var gameStateDto = new GameStateDto
            {
                PlayerState = new PlayerStateDto
                {
                    CurrentScore = player.PlayerState.CurrentScore,
                    TotalLifeTimeScoreEarned = player.PlayerState.TotalLifeTimeScoreEarned,
                    GoldBars = player.PlayerState.GoldBars,
                    PrestigeCount = player.PlayerState.PrestigeCount,
                    LastSaveTimestamp = player.PlayerState.LastSaveTimestamp,
                    StoredOfflineTimeSeconds = player.PlayerState.StoredOfflineTimeSeconds,
                    MaxOfflineStorageHours = player.PlayerState.MaxOfflineStorageHours,
                    TimePerClickSecond = player.PlayerState.TimePerClickSecond
                },
                PlayerSettings = new PlayerSettingsDto
                {
                    MusicVolume = player.PlayerSettings.MusicVolume,
                    SfxVolume = player.PlayerSettings.SfxVolume,
                    NotificationsEnabled = player.PlayerSettings.NotificationsEnabled
                },
                PlayerChatInfo = new PlayerChatInfoDto
                {
                    ChatUsername = player.PlayerChatInfo.ChatUsername
                },
                PlayerAgeVerification = new PlayerAgeVerificationDto
                {
                    IsVerified = player.PlayerAgeVerification?.AgeVerificationStatus?.Status == "Verified" 
                },
                PlayerUpgrades = player.PlayerUpgrades.Select(pu => new PlayerUpgradeDto
                {
                    UpgradeId = pu.UpgradeId,
                    Level = pu.Level
                }).ToList(),
                PlayerAchievements = player.PlayerAchievements.Select(pa => new PlayerAchievementDto
                {
                    AchievementId = pa.AchievementId,
                    UnlockedAt = pa.UnlockedAt
                }).ToList(),
                PlayerStatistics = player.PlayerStatistics.Select(ps => new PlayerStatisticDto
                {
                    StatisticDefinitionId = ps.StatisticDefinitionId,
                    NumericValue = ps.NumericValue
                }).ToList()
            };

            return Ok(gameStateDto);
        }

        // PUT: api/players/{playerId}/gameState
        [HttpPut("{playerId:long}/gameState")]
        public async Task<IActionResult> UpdateGameState(long playerId, [FromBody] GameStateDto gameStateDto)
        {
            var player = await _context.Players
                .Include(p => p.PlayerState)
                .Include(p => p.PlayerSettings)
                .Include(p => p.PlayerChatInfo) // Include Chat Info
                .Include(p => p.PlayerAgeVerification) // Include Age Verification
                .Include(p => p.PlayerUpgrades)
                .Include(p => p.PlayerAchievements)
                .Include(p => p.PlayerStatistics)
                // Not including MutedPlayers
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null)
            {
                return NotFound($"Player {playerId} not found.");
            }

            if (player.PlayerState == null || player.PlayerSettings == null || player.PlayerChatInfo == null || player.PlayerAgeVerification == null)
            {
                return StatusCode(500, "Player data is incomplete. Cannot save state.");
            }

            // --- Update PlayerState ---
            var stateDto = gameStateDto.PlayerState;
            player.PlayerState.CurrentScore = stateDto.CurrentScore;
            player.PlayerState.TotalLifeTimeScoreEarned = stateDto.TotalLifeTimeScoreEarned;
            player.PlayerState.GoldBars = stateDto.GoldBars;
            player.PlayerState.PrestigeCount = stateDto.PrestigeCount;
            player.PlayerState.LastSaveTimestamp = DateTime.UtcNow;
            player.PlayerState.StoredOfflineTimeSeconds = stateDto.StoredOfflineTimeSeconds;
            player.PlayerState.MaxOfflineStorageHours = stateDto.MaxOfflineStorageHours;
            player.PlayerState.TimePerClickSecond = stateDto.TimePerClickSecond;
            player.PlayerState.UpdatedAt = DateTime.UtcNow;

            // --- Update PlayerSettings ---
            var settingsDto = gameStateDto.PlayerSettings;
            player.PlayerSettings.MusicVolume = settingsDto.MusicVolume;
            player.PlayerSettings.SfxVolume = settingsDto.SfxVolume;
            player.PlayerSettings.NotificationsEnabled = settingsDto.NotificationsEnabled;
            player.PlayerSettings.UpdatedAt = DateTime.UtcNow;

            // --- Update PlayerChatInfo ---
            var chatInfoDto = gameStateDto.PlayerChatInfo;
            player.PlayerChatInfo.ChatUsername = chatInfoDto.ChatUsername;
            player.PlayerChatInfo.UpdatedAt = DateTime.UtcNow;

            // --- Update PlayerAgeVerification ---
            // This might require more complex logic. 
            // For now, let's assume we just store the client's claimed status.
            // We'd need to load the relevant AgeVerificationStatus ID ("Verified" or "Not Verified")
            var ageDto = gameStateDto.PlayerAgeVerification;
            // Example: Find the ID for the status based on ageDto.IsVerified 
            // long targetStatusId = await _context.AgeVerificationStatuses
            //     .Where(s => s.Status == (ageDto.IsVerified ? "Verified" : "Not Verified"))
            //     .Select(s => s.Id)
            //     .FirstOrDefaultAsync();
            // if (targetStatusId != 0) { 
            //     player.PlayerAgeVerification.AgeVerificationStatusId = targetStatusId;
            //     player.PlayerAgeVerification.VerifiedAt = ageDto.IsVerified ? (player.PlayerAgeVerification.VerifiedAt ?? DateTime.UtcNow) : null; // Set timestamp only if verified
            //     player.PlayerAgeVerification.UpdatedAt = DateTime.UtcNow;
            // }
            // Simplifying for now - assuming verification logic is handled elsewhere or not updated via this endpoint
            // Potential TODO: Add dedicated endpoint for age verification update?

            // --- Update PlayerUpgrades (Remove & Add approach) ---
            _context.PlayerUpgrades.RemoveRange(player.PlayerUpgrades);
            player.PlayerUpgrades.Clear();
            foreach (var upgradeDto in gameStateDto.PlayerUpgrades)
            {
                player.PlayerUpgrades.Add(new PlayerUpgrade
                {
                    PlayerId = playerId,
                    UpgradeId = upgradeDto.UpgradeId,
                    Level = upgradeDto.Level,
                    PurchasedAt = DateTime.UtcNow, // Assuming new/updated upgrades are 'purchased' now
                    LastLeveledAt = DateTime.UtcNow
                });
            }

            // --- Update PlayerAchievements (Remove & Add approach) ---
            _context.PlayerAchievements.RemoveRange(player.PlayerAchievements);
            player.PlayerAchievements.Clear();
            foreach (var achievementDto in gameStateDto.PlayerAchievements)
            {
                player.PlayerAchievements.Add(new PlayerAchievement
                {
                    PlayerId = playerId,
                    AchievementId = achievementDto.AchievementId,
                    UnlockedAt = achievementDto.UnlockedAt, // Use timestamp from client if meaningful
                    RewardClaimed = false // Default or handle based on DTO if claim status is sent
                });
            }

            // --- Update PlayerStatistics (Remove & Add approach) ---
            _context.PlayerStatistics.RemoveRange(player.PlayerStatistics);
            player.PlayerStatistics.Clear();
            foreach (var statisticDto in gameStateDto.PlayerStatistics)
            {
                player.PlayerStatistics.Add(new PlayerStatistic
                {
                    PlayerId = playerId,
                    StatisticDefinitionId = statisticDto.StatisticDefinitionId,
                    NumericValue = statisticDto.NumericValue,
                    LastUpdatedAt = DateTime.UtcNow
                });
            }

            // Update Player's LastLoginAt
            player.LastLoginAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle potential concurrency issues if necessary
                if (!await PlayerExists(playerId))
                {
                    return NotFound($"Player {playerId} not found during save.");
                }
                else
                {
                    throw; // Re-throw if it's another concurrency issue
                }
            }
            catch (Exception ex)
            {
                // Log the exception ex
                Console.WriteLine($"ERROR updating game state for player {playerId}: {ex.Message}"); // Basic console logging
                // Consider using a proper logging framework
                return StatusCode(500, "An error occurred while saving game state.");
            }

            return NoContent(); // Success
        }

        // DELETE: api/players/{playerId}
        [HttpDelete("{playerId:long}")]
        public async Task<IActionResult> DeletePlayer(long playerId)
        {
            // Find the player entity
            var player = await _context.Players.FindAsync(playerId);

            if (player == null)
            {
                // Return NotFound if the player doesn't exist
                return NotFound($"Player {playerId} not found.");
            }

            // Remove the player entity
            // EF Core will handle removing related data based on cascade delete settings
            _context.Players.Remove(player);

            try
            {
                // Save changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) // Catch potential FK constraint issues if cascade isn't perfect
            {
                // Log the exception (ex)
                Console.WriteLine($"ERROR deleting player {playerId}: {ex.Message}"); // Basic console logging
                // Consider using a proper logging framework
                return StatusCode(500, $"An error occurred while deleting player {playerId}. Dependent data might still exist.");
            }

            // Return NoContent to indicate successful deletion
            return NoContent(); 
        }

        // GET: api/players/ping
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            // Simple endpoint to check if the API is responding
            return Ok("Pong!");
        }

        // --- Specific Update Endpoints --- 

        // PUT: api/players/{playerId}/chatInfo
        [HttpPut("{playerId:long}/chatInfo")]
        public async Task<IActionResult> UpdateChatInfo(long playerId, [FromBody] UpdateUsernameRequestDto requestDto)
        {
            if (requestDto == null || string.IsNullOrWhiteSpace(requestDto.ChatUsername))
            {
                 return BadRequest("Invalid username data provided.");
            }
            
            // Find the player and their chat info
            var playerChatInfo = await _context.PlayerChatInfos
                                         .FirstOrDefaultAsync(pci => pci.PlayerId == playerId);

            if (playerChatInfo == null)
            {
                 // This might happen if the player record exists but the relation wasn't created? Unlikely with current setup.
                 // Or, the player simply doesn't exist.
                 if (!await PlayerExists(playerId)){
                     return NotFound($"Player {playerId} not found.");
                 } else {
                     // Player exists but info doesn't - potentially create it?
                     // For now, return error. Could create it if needed.
                     return StatusCode(500, $"Player chat info record not found for player {playerId}.");
                 }
            }

            // Validate length etc. again server-side? (DTO attributes handle basic validation)
            string validatedUsername = requestDto.ChatUsername.Trim();
            if (validatedUsername.Length > 20) validatedUsername = validatedUsername.Substring(0, 20);
            if (string.IsNullOrEmpty(validatedUsername)) return BadRequest("Username cannot be empty after trimming.");

            // Update and save
            playerChatInfo.ChatUsername = validatedUsername;
            playerChatInfo.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) // Handle potential race conditions
            {
                 if (!await PlayerExists(playerId)) return NotFound();
                 else throw;
            }
            catch(Exception ex) {
                 Console.WriteLine($"ERROR updating chat info for player {playerId}: {ex.Message}");
                 return StatusCode(500, "An error occurred updating username.");
            }

            return NoContent(); // Success (204)
        }

        // POST: api/players/{playerId}/verifyAge
        [HttpPost("{playerId:long}/verifyAge")]
        public async Task<IActionResult> VerifyPlayerAge(long playerId)
        {
            // Find the player's age verification record
            var playerAgeVerification = await _context.PlayerAgeVerifications
                                                .FirstOrDefaultAsync(pav => pav.PlayerId == playerId);

            if (playerAgeVerification == null)
            {
                if (!await PlayerExists(playerId)) return NotFound($"Player {playerId} not found.");
                else return StatusCode(500, $"Player age verification record not found for player {playerId}.");
            }

            // Find the ID for the "Verified" status (assuming it exists from seeding)
            long verifiedStatusId = await _context.AgeVerificationStatuses
                                            .Where(s => s.Status == "Verified")
                                            .Select(s => s.Id)
                                            .FirstOrDefaultAsync();

            if (verifiedStatusId == 0) {
                // This indicates a seeding issue or data problem
                Console.WriteLine($"ERROR verifying age for player {playerId}: 'Verified' status not found in DB.");
                return StatusCode(500, "Server configuration error: Cannot find 'Verified' status.");
            }

            // Update the player's verification status
            playerAgeVerification.AgeVerificationStatusId = verifiedStatusId;
            playerAgeVerification.VerifiedAt = DateTime.UtcNow;
            // Optionally update VerificationMethod or AttemptCount if needed

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"ERROR saving age verification for player {playerId}: {ex.Message}");
                 return StatusCode(500, "An error occurred saving age verification.");
            }

            return Ok(); // Return 200 OK to indicate success
        }

        // --- Mute List Endpoints ---

        // GET: api/players/{playerId}/muted
        [HttpGet("{playerId:long}/muted")]
        public async Task<ActionResult<List<long>>> GetMutedPlayers(long playerId)
        {
            if (!await PlayerExists(playerId)) return NotFound($"Player {playerId} not found.");

            // Find players MUTED BY the specified playerId
            var mutedIds = await _context.MutedPlayers
                                     .Where(mp => mp.MuterPlayerId == playerId)
                                     .Select(mp => mp.MutedPlayerId) // Select only the ID of the muted player
                                     .ToListAsync();
            
            return Ok(mutedIds);
        }

        // POST: api/players/{muterPlayerId}/muted/{targetPlayerId}
        [HttpPost("{muterPlayerId:long}/muted/{targetPlayerId:long}")]
        public async Task<IActionResult> MutePlayer(long muterPlayerId, long targetPlayerId)
        {
            if (muterPlayerId == targetPlayerId) return BadRequest("Cannot mute yourself.");
            
            // Ensure both players exist
            if (!await PlayerExists(muterPlayerId)) return NotFound($"Muting player {muterPlayerId} not found.");
            if (!await PlayerExists(targetPlayerId)) return NotFound($"Target player {targetPlayerId} not found.");

            // Check if already muted
            bool alreadyMuted = await _context.MutedPlayers
                                        .AnyAsync(mp => mp.MuterPlayerId == muterPlayerId && mp.MutedPlayerId == targetPlayerId);

            if (alreadyMuted)
            {
                return Conflict("Player already muted."); // Or return Ok() if idempotent is preferred
            }

            // Create and add the mute record
            var muteRecord = new MutedPlayer
            {
                MuterPlayerId = muterPlayerId,
                MutedPlayerId = targetPlayerId,
                MutedAt = DateTime.UtcNow
                // Duration could be added here if needed
            };
            _context.MutedPlayers.Add(muteRecord);

            try {
                await _context.SaveChangesAsync();
                return Ok(); // Return 200 OK on successful mute
            }
            catch (Exception ex) {
                Console.WriteLine($"ERROR muting player {targetPlayerId} by {muterPlayerId}: {ex.Message}");
                return StatusCode(500, "An error occurred while muting player.");
            }
        }

        // DELETE: api/players/{muterPlayerId}/muted/{targetPlayerId}
        [HttpDelete("{muterPlayerId:long}/muted/{targetPlayerId:long}")]
        public async Task<IActionResult> UnmutePlayer(long muterPlayerId, long targetPlayerId)
        {
            // Find the mute record
            var muteRecord = await _context.MutedPlayers
                                       .FirstOrDefaultAsync(mp => mp.MuterPlayerId == muterPlayerId && mp.MutedPlayerId == targetPlayerId);

            if (muteRecord == null)
            {
                return NotFound("Mute record not found.");
            }

            _context.MutedPlayers.Remove(muteRecord);

             try {
                await _context.SaveChangesAsync();
                return NoContent(); // Return 204 No Content on successful unmute
            }
            catch (Exception ex) {
                Console.WriteLine($"ERROR unmuting player {targetPlayerId} by {muterPlayerId}: {ex.Message}");
                return StatusCode(500, "An error occurred while unmuting player.");
            }
        }

    }
}