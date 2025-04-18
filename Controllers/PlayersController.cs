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

        // DTO for the FindByDevice request body (Moved from PlayerController)
        public class FindByDeviceRequest
        {
            public required string DeviceId { get; set; }
        }

        // POST /api/players/findByDevice (Moved from PlayerController)
        [HttpPost("findByDevice")]
        public async Task<ActionResult<Player>> FindByDevice([FromBody] FindByDeviceRequest request)
        {
            if (string.IsNullOrEmpty(request.DeviceId))
            {
                return BadRequest("DeviceId cannot be empty.");
            }

            // Try to find an existing player by DeviceId
            // Consider including essential related data needed immediately after login/identification
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.DeviceId == request.DeviceId);

            if (player != null)
            {
                // Player found, update LastLoginAt and return
                player.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                // Return the basic player object for now. GameState will be fetched separately.
                return Ok(player); 
            }
            else
            {
                // Player not found, create a new one
                 // ---> ENHANCEMENT: Create associated default entities (State, Settings, ChatInfo, AgeVerification) here! <---
                var newPlayer = new Player
                {
                    DeviceId = request.DeviceId,
                    FirebaseUid = string.Empty, 
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow,
                    // Initialize required related entities with default values
                    PlayerState = new PlayerState(),
                    PlayerSettings = new PlayerSettings(),
                    PlayerChatInfo = new PlayerChatInfo { ChatUsername = null, UpdatedAt = DateTime.UtcNow }, // Add default ChatInfo
                    PlayerAgeVerification = new PlayerAgeVerification { AgeVerificationStatusId = 1, VerificationAttemptCount = 0 } // Add default AgeVerification (linking to Status ID 1: Not Verified)
                };

                _context.Players.Add(newPlayer);
                await _context.SaveChangesAsync();

                // Return 201 Created with the new player data (including the generated ID)
                return CreatedAtAction(nameof(GetPlayerById), new { id = newPlayer.PlayerId }, newPlayer);
            }
        }


        // GET: api/players/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Player>> GetPlayerById(long id)
        {
            // Basic fetch, GameState fetch will be separate
            var player = await _context.Players
                                     .FirstOrDefaultAsync(p => p.PlayerId == id);

            if (player == null)
            {
                return NotFound($"Player {id} not found");
            }

            // Return just the player object. Client should call GetGameState if needed.
            return Ok(player);
        }


        //POST - CREATE api/players
        // TODO: This is a basic implementation. Creating a player likely requires
        // ... existing comments ...
        // NOTE: This endpoint might become less relevant if FindByDevice handles creation.
        [HttpPost]
        public async Task<ActionResult<Player>> CreatePlayer([FromBody] Player newPlayer) // Input type changed
        {
            if (newPlayer == null)
            {
                return BadRequest("Player data is null");
            }

            // Basic validation example - Ensure required fields are present
            if (string.IsNullOrEmpty(newPlayer.FirebaseUid))
            {
                 return BadRequest("FirebaseUid is required.");
            }

            // Check if FirebaseUid already exists (assuming it must be unique)
            if (await _context.Players.AnyAsync(p => p.FirebaseUid == newPlayer.FirebaseUid))
            {
                 return Conflict($"Player with FirebaseUid {newPlayer.FirebaseUid} already exists.");
            }

            // Set default timestamps (though model defaults might handle this)
            newPlayer.CreatedAt = DateTime.UtcNow;
            newPlayer.LastLoginAt = DateTime.UtcNow;

            // >>> Enhancement Needed <<< 
            // Ensure associated entities are created here too, similar to FindByDevice
            if(newPlayer.PlayerState == null) newPlayer.PlayerState = new PlayerState();
            if(newPlayer.PlayerSettings == null) newPlayer.PlayerSettings = new PlayerSettings();
            // ... etc ...

            _context.Players.Add(newPlayer); // Use Players DbSet

            await _context.SaveChangesAsync();

            // Return 201 Created, referencing the GetPlayerById action
            return CreatedAtAction(nameof(GetPlayerById), new { id = newPlayer.PlayerId }, newPlayer);
        }


        //PUT - UPDATE api/players/{id}
        // TODO: This is a basic implementation. Updating related entities (PlayerState, etc.)
        // ... existing comments ...
        // NOTE: This endpoint is likely superseded by the PUT GameState endpoint.
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePlayer(long id, [FromBody] Player playerUpdateData) // id is long, input is Player
        {
            if (playerUpdateData == null)
            {
                return BadRequest("Player data is null");
            }

            // Ignore PlayerId from body, use the one from the route
            // if (playerUpdateData.PlayerId != 0 && playerUpdateData.PlayerId != id)
            // {
            //     return BadRequest("Player ID mismatch in body vs route.");
            // }

            var existingPlayer = await _context.Players.FindAsync(id); // Find Player by long id
            if (existingPlayer == null)
            {
                return NotFound($"Player not found with ID {id}");
            }

            // Update only allowed fields from playerUpdateData
            // Example: Updating ChatDeviceId. Add other updatable Player fields as needed.
            if (playerUpdateData.ChatDeviceId != null)
            {
                existingPlayer.ChatDeviceId = playerUpdateData.ChatDeviceId;
            }
            // existingPlayer.FirebaseId = playerUpdateData.FirebaseId; // If FirebaseId is updatable

            // DO NOT update related entities here unless intended and handled carefully.
            // Updates to PlayerState, PlayerSettings etc. should have their own endpoints/logic.
            // existingPlayer.PlayerState.CurrentScore = playerUpdateData.PlayerState.CurrentScore; // <-- Avoid this pattern here

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await PlayerExists(id))
                {
                    return NotFound($"Player {id} not found during save.");
                }
                else
                {
                    throw;
                }
            }
            return NoContent(); // Return 204 No Content on successful update
        }

        // Helper method updated for Player
        private async Task<bool> PlayerExists(long id) // id is long
        {
            return await _context.Players.AnyAsync(e => e.PlayerId == id); // Check Players DbSet
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
    }
}