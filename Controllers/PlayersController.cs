using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data.Context;
using Api.Data.Models;
using System.Threading.Tasks; // Ensure this is present
using Api.Data.Dtos; // Add this using statement for DTOs
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using Microsoft.Extensions.Logging; // Added for ILogger
using Microsoft.IdentityModel.Tokens; // Added for JWT
using System.IdentityModel.Tokens.Jwt; // Added for JWT
using System.Security.Claims; // Added for JWT
using System.Text; // Added for JWT
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Cryptography; // Added for Refresh Token generation
using System.Security.Claims; // Added for ClaimTypes

namespace Api.Controllers
{
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Added
        private readonly ILogger<PlayersController> _logger; // Added for ILogger

        // Inject IConfiguration and ILogger
        public PlayersController(AppDbContext context, IConfiguration configuration, ILogger<PlayersController> logger)
        {
            _context = context;
            _configuration = configuration; // Added
            _logger = logger; // Added
        }

        // --- DTOs for Controller Actions ---
        public class IdentifyPlayerRequestDto
        {
            public required string DeviceId { get; set; }
            public required string FirebaseUid { get; set; }
        }

        // DTO to include the ACCESS token in the response
        // Refresh token will be sent via HttpOnly Cookie
        public class IdentifyPlayerResponseDto
        {
            public required PlayerDto Player { get; set; }
            public required string AccessToken { get; set; }
        }

        public class RefreshTokenRequestDto
        {
            public string? ExpiredAccessToken { get; set; } // Optional: Send expired token for potential checks
        }

        public class RefreshTokenResponseDto
        {
            public required string AccessToken { get; set; }
        }

        public class UpdateUsernameRequestDto // Kept from original code
        {
            public required string ChatUsername { get; set; }
        }

        // --- Player Identification and Basic CRUD --- 

        // POST /api/players/identify
        [HttpPost("identify")]
        public async Task<ActionResult<IdentifyPlayerResponseDto>> IdentifyPlayer([FromBody] IdentifyPlayerRequestDto request)
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

            // --- Generate Tokens and Save Refresh Token --- 
            var accessToken = GenerateJwtToken(player);
            var refreshToken = GenerateRefreshToken(); 

            // Set refresh token details on the player entity
            player.RefreshToken = refreshToken; // Store the actual token (or hash it)
            player.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Set expiry (e.g., 7 days)
            // -------------------------------------------------

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                 Console.WriteLine($"Error saving player identify/refresh token: {ex.InnerException?.Message ?? ex.Message}");
                 return Conflict("A conflict occurred while trying to save player data.");
            }

            var playerDto = MapPlayerToDto(player);

            // --- Set Refresh Token in Secure, HttpOnly Cookie --- 
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Prevents client-side script access
                Expires = player.RefreshTokenExpiryTime, // Cookie expires with the token
                // Secure = true, // Set to true if using HTTPS
                // SameSite = SameSiteMode.Strict // Consider Strict or Lax for CSRF protection
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            // -----------------------------------------------------

            var responseDto = new IdentifyPlayerResponseDto
            {
                Player = playerDto,
                AccessToken = accessToken // Only return the access token in the body
            };

            if (createdNewPlayer)
            {
                return CreatedAtAction(nameof(GetPlayerById), new { id = player.PlayerId }, responseDto);
            }
            else
            {
                return Ok(responseDto);
            }
        }

        // --- ADDED: Refresh Token Endpoint --- 
        // POST /api/players/refresh
        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshTokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto requestDto) // Added request DTO if needed
        {
            // 1. Get refresh token from HttpOnly cookie
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Unauthorized("Refresh token not found.");
            }

            // 2. Find player by refresh token (assuming we store the plain token for simplicity)
            //    NOTE: Storing a HASH of the refresh token is more secure.
            var player = await _context.Players.FirstOrDefaultAsync(p => p.RefreshToken == refreshToken);

            // 3. Validate token existence and expiry
            if (player == null || player.RefreshTokenExpiryTime == null || player.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Invalid or expired refresh token.");
            }

            // 4. Generate a new access token
            var newAccessToken = GenerateJwtToken(player);

            // --- OPTIONAL: Refresh Token Rotation --- 
            // Generate a new refresh token and update expiry
            // var newRefreshToken = GenerateRefreshToken();
            // player.RefreshToken = newRefreshToken;
            // player.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Reset expiry
            // await _context.SaveChangesAsync(); // Save the new refresh token
            // // Set the new refresh token in the cookie
            // var cookieOptions = new CookieOptions { HttpOnly = true, Expires = player.RefreshTokenExpiryTime };
            // Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);
            // --- End Optional Rotation ---

            // 5. Return the new access token
            return Ok(new RefreshTokenResponseDto { AccessToken = newAccessToken });
        }
        // ---------------------------------------

        // Helper method to generate JWT token (Access Token)
        private string GenerateJwtToken(Player player)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["SecretKey"];
            if (string.IsNullOrEmpty(secretKey)) {
                throw new InvalidOperationException("JWT Secret Key not configured.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create claims (information about the user)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, player.PlayerId.ToString()), // Subject = Player ID
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique Token ID
                new Claim("FirebaseUid", player.FirebaseUid ?? string.Empty), // Custom claim for Firebase UID
                // Add other claims as needed (e.g., roles)
            };

            // Define token details
            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12), // CHANGED: 12 Hour Expiry 
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Helper method to generate a secure random string for Refresh Token
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64]; // Increased size for more entropy
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // GET: api/players/{id}
        [HttpGet("{id:long}")]
        [Authorize] // Secure this endpoint
        public async Task<ActionResult<PlayerDto>> GetPlayerById(long id)
        {
            // Optional: Check if the authenticated user (from token) matches the requested id
            var requestingPlayerId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (requestingPlayerId != id.ToString()) {
                // return Forbid(); // Or Unauthorized()
            }
            // End Optional Check

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

        // POST: api/players/findByDevice
        [HttpPost("findByDevice")]
        public async Task<ActionResult<PlayerDto>> FindByDevice([FromBody] DeviceIdRequest request)
        {
            if (string.IsNullOrEmpty(request.deviceId))
            {
                return BadRequest("Device ID cannot be empty.");
            }

            var player = await _context.Players
                                     .AsNoTracking()
                                     .Where(p => p.DeviceId == request.deviceId)
                                     .FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound($"Player not found with Device ID: {request.deviceId}");
            }

            return Ok(MapPlayerToDto(player));
        }

        // Add the Request DTO class for FindByDevice endpoint
        public class DeviceIdRequest
        {
            public string deviceId { get; set; } = string.Empty;
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
        [Authorize] // Secure this endpoint
        public async Task<ActionResult<GameStateDto>> GetGameState(long playerId)
        {
            // Validate requester matches player ID using ClaimTypes.NameIdentifier
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            // --- Add Logging ---
            _logger.LogInformation($"GameState AuthCheck: Token Sub='{requestingPlayerId ?? "NULL"}', URL PlayerId='{playerId}'");
            // -----------------------------------
            if (requestingPlayerId != playerId.ToString())
            {
                _logger.LogWarning($"GameState AuthCheck FAILED for PlayerId {playerId}. Token Sub was '{requestingPlayerId ?? "NULL"}'. Returning Forbid.");
                return Forbid();
            }
            _logger.LogInformation($"GameState AuthCheck PASSED for PlayerId {playerId}.");

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
                    // Explicitly check each part, add null-forgiving (!) to satisfy compiler
                    IsVerified = player.PlayerAgeVerification != null && 
                                 player.PlayerAgeVerification.AgeVerificationStatus != null &&
                                 player.PlayerAgeVerification.AgeVerificationStatus!.Status == "Verified"
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
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> UpdateGameState(long playerId, [FromBody] GameStateDto gameStateDto)
        {
            // Validate requester matches player ID
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier); // This should now correctly get "3"
            
            // --- ADD DETAILED LOGGING FOR PUT --- 
            _logger.LogInformation($"UpdateGameState AuthCheck: Comparing Token Sub='{requestingPlayerId ?? "NULL"}' with URL PlayerId='{playerId}' (as string: '{playerId.ToString()}')");
            // ------------------------------------
            
            if (requestingPlayerId != playerId.ToString()) 
            {
                _logger.LogWarning($"UpdateGameState AuthCheck FAILED. Token Sub '{requestingPlayerId ?? "NULL"}' != URL PlayerId '{playerId}'. Returning Forbid.");
                return Forbid(); // <<< This is the check causing the 403
            }
            _logger.LogInformation("UpdateGameState AuthCheck PASSED.");

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
            // REMOVED: Username update is now handled by a dedicated API call
            // var chatInfoDto = gameStateDto.PlayerChatInfo;
            // player.PlayerChatInfo.ChatUsername = chatInfoDto.ChatUsername;
            // player.PlayerChatInfo.UpdatedAt = DateTime.UtcNow;

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

            // Explicitly detach PlayerChatInfo to prevent implicit updates during the final save
            if (player.PlayerChatInfo != null) // Add null check for safety
            {
                 _context.Entry(player.PlayerChatInfo).State = EntityState.Detached;
            }

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
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> DeletePlayer(long playerId)
        {
            // Validate requester matches player ID
            var requestingPlayerId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (requestingPlayerId != playerId.ToString()) return Forbid();

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
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> UpdateChatInfo(long playerId, [FromBody] UpdateUsernameRequestDto requestDto)
        {
            // Validate requester matches player ID
            var requestingPlayerId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (requestingPlayerId != playerId.ToString()) return Forbid();

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
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> VerifyPlayerAge(long playerId)
        {
            // Validate requester matches player ID
            var requestingPlayerId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (requestingPlayerId != playerId.ToString()) return Forbid();

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
        [Authorize] // Secure this endpoint
        public async Task<ActionResult<List<long>>> GetMutedPlayers(long playerId)
        {
            // Validate requester matches player ID using ClaimTypes.NameIdentifier
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // --- Add Logging ---
            _logger.LogInformation($"MutedList AuthCheck: Token Sub='{requestingPlayerId ?? "NULL"}', URL PlayerId='{playerId}'");
            // -----------------------------------
            if (requestingPlayerId != playerId.ToString())
            {
                 _logger.LogWarning($"MutedList AuthCheck FAILED for PlayerId {playerId}. Token Sub was '{requestingPlayerId ?? "NULL"}'. Returning Forbid.");
                 return Forbid();
            }
            _logger.LogInformation($"MutedList AuthCheck PASSED for PlayerId {playerId}.");

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
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> MutePlayer(long muterPlayerId, long targetPlayerId)
        {
            // Validate requester matches muterPlayerId
            var requestingPlayerId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (requestingPlayerId != muterPlayerId.ToString()) return Forbid();

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
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> UnmutePlayer(long muterPlayerId, long targetPlayerId)
        {
            // Validate requester matches muterPlayerId
            var requestingPlayerId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (requestingPlayerId != muterPlayerId.ToString()) return Forbid();

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