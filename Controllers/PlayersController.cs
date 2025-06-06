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
using System.Text; // Added for JWT
using Microsoft.AspNetCore.Authorization; // Added for [Authorize]
using System.Security.Cryptography; // Added for Refresh Token generation
using System.Security.Claims; // Added for ClaimTypes
using System.Globalization; // For CultureInfo.InvariantCulture
using System.Linq;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/players")]
    public class PlayersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration; // Added
        private readonly ILogger<PlayersController> _logger; // Added for ILogger

        // Constants for MemeMint - LATER, move to server configuration or Upgrade entities
        private const decimal MINT_COST_GOLD_BARS = 50000M;
        private const float MINT_BASE_DURATION_SECONDS = 3600f;
        private const int MINT_PROGRESS_PER_CYCLE = 100;
        private const int MINT_TOTAL_PROGRESS_FOR_BATCH = 600; // Adjusted to 200 for UI screenshot
        private const decimal MINT_GCM_POINTS_PER_BATCH = 5M;

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

        public class VerifyPlayerAgeRequestDto
        {
            public DateTime DateOfBirth { get; set; }
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
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            _logger.LogInformation($"GameState AuthCheck: Token Sub='{requestingPlayerId ?? "NULL"}', URL PlayerId='{playerId}'"); 
            if (requestingPlayerId != playerId.ToString())
            {
                _logger.LogWarning($"GameState AuthCheck FAILED for PlayerId {playerId}. Token Sub was '{requestingPlayerId ?? "NULL"}'. Returning Forbid.");
                return Forbid();
            }
            _logger.LogInformation($"GameState AuthCheck PASSED for PlayerId {playerId}.");

            var player = await _context.Players
                .Include(p => p.PlayerState)
                .Include(p => p.PlayerSettings)
                .Include(p => p.PlayerChatInfo)
                .Include(p => p.PlayerAgeVerification).ThenInclude(pav => pav.AgeVerificationStatus)
                .Include(p => p.PlayerUpgrades)
                .Include(p => p.PlayerAchievements)
                .Include(p => p.PlayerStatistics)
                .Include(p => p.MemeMintPlayerData)
                    .ThenInclude(mmpd => mmpd.MinterInstances)
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null)
            {
                return NotFound($"Player {playerId} not found.");
            }

            if (player.PlayerState == null || player.PlayerSettings == null || player.PlayerChatInfo == null || player.PlayerAgeVerification == null)
            {
                return StatusCode(500, "Player data is incomplete. Missing required state, settings, chat info, or age verification.");
            }

            // Ensure MemeMintPlayerData and default minters exist
            if (player.MemeMintPlayerData == null)
            {
                _logger.LogInformation($"Player {playerId} has no MemeMintPlayerData. Initializing defaults.");
                player.MemeMintPlayerData = new PlayerMemeMintPlayerData { PlayerId = playerId, CreatedAt = DateTime.UtcNow };
                InitializeDefaultMinterInstances(player.MemeMintPlayerData);
                await _context.SaveChangesAsync(); 
            }
            else if (player.MemeMintPlayerData.MinterInstances == null || 
                     !player.MemeMintPlayerData.MinterInstances.Any() || 
                     player.MemeMintPlayerData.MinterInstances.Count < 3) // Ensure we check for at least 3 for safety
            {
                 _logger.LogInformation($"Player {playerId} MemeMintPlayerData exists but MinterInstances are missing/incomplete ({player.MemeMintPlayerData.MinterInstances?.Count ?? 0} found). Re-initializing defaults.");
                 InitializeDefaultMinterInstances(player.MemeMintPlayerData); 
                 await _context.SaveChangesAsync();
            }

            // --- SERVER-SIDE OFFLINE MINTER PROGRESSION (before mapping to DTO) ---
            if (player.PlayerState != null && player.PlayerState.LastSaveTimestamp > DateTime.MinValue) // Ensure PlayerState exists
            {
                TimeSpan offlineTimeSpan = DateTime.UtcNow - player.PlayerState.LastSaveTimestamp;
                float offlineDurationSeconds = (float)offlineTimeSpan.TotalSeconds;
                if (offlineDurationSeconds > 10) 
                {
                    _logger.LogInformation($"Player {playerId} was offline for {offlineDurationSeconds:F1}s. Processing MemeMint offline logic.");
                    // <<< Log state of Minter 1 BEFORE offline processing (if it exists) >>>
                    var minter1BeforeOffline = player.MemeMintPlayerData?.MinterInstances?.FirstOrDefault(m => m.ClientInstanceId == 1);
                    if (minter1BeforeOffline != null) _logger.LogInformation($"[GetGameState] Minter 1 BEFORE OfflineProcessing: State={minter1BeforeOffline.State}, TimeLeft={minter1BeforeOffline.TimeRemainingSeconds}");
                    else _logger.LogWarning("[GetGameState] Minter 1 not found before offline processing.");

                    bool changedByOffline = await ProcessAndUpdateMinterCyclesInternal(player, offlineDurationSeconds, true);
                    if (changedByOffline) 
                    { 
                        // <<< Log state of Minter 1 AFTER ProcessAndUpdateMinterCyclesInternal (in memory) but BEFORE SaveChanges >>>
                        var minter1AfterProcess = player.MemeMintPlayerData?.MinterInstances?.FirstOrDefault(m => m.ClientInstanceId == 1);
                        if (minter1AfterProcess != null) _logger.LogInformation($"[GetGameState] Minter 1 AFTER ProcessAndUpdateMinterCyclesInternal (pre-save): State={minter1AfterProcess.State}");
                        
                        await _context.SaveChangesAsync(); 
                        _logger.LogInformation($"Saved changes after server-side offline MemeMint processing for player {playerId}.");

                        // <<< OPTIONAL: Re-fetch or ensure context is updated before DTO mapping if issues persist >>>
                        // player = await _context.Players.Include(...all includes again...).FirstOrDefaultAsync(p => p.PlayerId == playerId); 
                        // _logger.LogInformation($"Player data re-fetched after offline save for PlayerId {playerId}");
                    }
                }
            }
            // --- END SERVER-SIDE OFFLINE MINTER PROGRESSION ---

            // <<< Log state of Minter 1 and instance count IMMEDIATELY BEFORE DTO MAPPING >>>
            _logger.LogInformation($"[GetGameState] PRE-DTO MAPPING: MemeMintPlayerData.MinterInstances count: {player.MemeMintPlayerData?.MinterInstances?.Count ?? -1}");
            var minter1ForDto = player.MemeMintPlayerData?.MinterInstances?.FirstOrDefault(m => m.ClientInstanceId == 1);
            if (minter1ForDto != null) _logger.LogInformation($"[GetGameState] Minter 1 FOR DTO MAPPING: ID={minter1ForDto.ClientInstanceId}, EntityState={minter1ForDto.State}, TimeLeft={minter1ForDto.TimeRemainingSeconds}");
            else _logger.LogWarning("[GetGameState] Minter 1 NOT FOUND for DTO mapping.");

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
                PlayerAgeVerification = new Api.Data.Dtos.PlayerAgeVerificationDto
                {
                    // IsVerified means a DoB was submitted and client-side platform check passed.
                    IsVerified = player.PlayerAgeVerification?.AgeVerificationStatus?.Status == "Verified",
                    DateOfBirth = player.PlayerAgeVerification?.DateOfBirth, // Populate DateOfBirth
                    IsOver18 = CalculateIsOver18(player.PlayerAgeVerification?.DateOfBirth) // Calculate IsOver18
                },
                PlayerUpgrades = player.PlayerUpgrades?.Select(pu => new PlayerUpgradeDto
                {
                    UpgradeId = pu.UpgradeId,
                    Level = pu.Level
                }).ToList() ?? new List<PlayerUpgradeDto>(),
                PlayerAchievements = player.PlayerAchievements?.Select(pa => new PlayerAchievementDto
                {
                    AchievementId = pa.AchievementId,
                    UnlockedAt = pa.UnlockedAt
                }).ToList() ?? new List<PlayerAchievementDto>(),
                PlayerStatistics = player.PlayerStatistics.Select(ps => new PlayerStatisticDto
                {
                    StatisticDefinitionId = ps.StatisticDefinitionId,
                    NumericValue = ps.NumericValue
                }).ToList(),

                MemeMintData = player.MemeMintPlayerData == null ? new MemeMintPlayerDataDto() : new MemeMintPlayerDataDto // Ensure DTO is not null
                {
                    PlayerGCMPMPoints = player.MemeMintPlayerData?.PlayerGCMPMPoints ?? 0,
                    SharedMintProgress = player.MemeMintPlayerData?.SharedMintProgress ?? 0,
                    MinterInstances = player.MemeMintPlayerData?.MinterInstances? // Null-conditional access
                        .Select(mi => 
                        {
                            float timeRemaining = mi.TimeRemainingSeconds; // Default to stored value
                            if (mi.State == MinterState.MintingInProgress && mi.LastCycleStartTimeUTC.HasValue)
                            {
                                float totalCycleDuration = GetBaseMintingTimeForInstance(mi.ClientInstanceId);
                                float elapsedSeconds = (float)(DateTime.UtcNow - mi.LastCycleStartTimeUTC.Value).TotalSeconds;
                                timeRemaining = Math.Max(0, totalCycleDuration - elapsedSeconds);
                            }

                            return new MinterInstanceDataDto
                            {
                                InstanceId = mi.ClientInstanceId,
                                State = (MinterStateDto)mi.State,
                                TimeRemainingSeconds = timeRemaining, // Use the dynamically calculated value
                                IsUnlocked = mi.IsUnlocked
                            };
                        }).ToList() ?? new List<MinterInstanceDataDto>() // Default to empty list if MinterInstances is null
                }
            };
            // Repopulate other DTO fields as they were
            gameStateDto.PlayerState.CurrentScore = player.PlayerState.CurrentScore;
            gameStateDto.PlayerState.TotalLifeTimeScoreEarned = player.PlayerState.TotalLifeTimeScoreEarned;
            gameStateDto.PlayerState.GoldBars = player.PlayerState.GoldBars;
            gameStateDto.PlayerState.PrestigeCount = player.PlayerState.PrestigeCount;
            gameStateDto.PlayerState.LastSaveTimestamp = player.PlayerState.LastSaveTimestamp;
            gameStateDto.PlayerState.StoredOfflineTimeSeconds = player.PlayerState.StoredOfflineTimeSeconds;
            gameStateDto.PlayerState.MaxOfflineStorageHours = player.PlayerState.MaxOfflineStorageHours;
            gameStateDto.PlayerState.TimePerClickSecond = player.PlayerState.TimePerClickSecond;

            gameStateDto.PlayerSettings.MusicVolume = player.PlayerSettings.MusicVolume;
            gameStateDto.PlayerSettings.SfxVolume = player.PlayerSettings.SfxVolume;
            gameStateDto.PlayerSettings.NotificationsEnabled = player.PlayerSettings.NotificationsEnabled;

            gameStateDto.PlayerChatInfo.ChatUsername = player.PlayerChatInfo?.ChatUsername;
            ((Api.Data.Dtos.PlayerAgeVerificationDto)gameStateDto.PlayerAgeVerification).IsVerified = player.PlayerAgeVerification?.AgeVerificationStatus?.Status == "Verified";
            ((Api.Data.Dtos.PlayerAgeVerificationDto)gameStateDto.PlayerAgeVerification).DateOfBirth = player.PlayerAgeVerification?.DateOfBirth;
            ((Api.Data.Dtos.PlayerAgeVerificationDto)gameStateDto.PlayerAgeVerification).IsOver18 = CalculateIsOver18(player.PlayerAgeVerification?.DateOfBirth);

            return Ok(gameStateDto);
        }

        // Server-authoritative processing of minter cycles and rewards
        private async Task<bool> ProcessAndUpdateMinterCyclesInternal(Player player, float? elapsedSecondsOverride = null, bool isOfflineCalculation = false)
        {
            if (player.MemeMintPlayerData == null) 
            {
                _logger.LogWarning($"ProcessMinterCycles: Player {player.PlayerId} has no MemeMintPlayerData.");
                return false; 
            }

            _logger.LogInformation($"[ProcessInternal] Processing Minter Cycles for Player {player.PlayerId}. isOffline: {isOfflineCalculation}, elapsedOverride: {elapsedSecondsOverride?.ToString("F1") ?? "N/A"}");

            bool anyChangeMade = false;
            DateTime currentTimeUtc = DateTime.UtcNow;

            foreach (var minter in player.MemeMintPlayerData.MinterInstances.ToList())
            {
                if (minter.State == MinterState.MintingInProgress && minter.IsUnlocked && minter.LastCycleStartTimeUTC.HasValue)
                {
                    // --- REVISED: Server-Authoritative Time Calculation ---
                    float totalCycleDuration = GetBaseMintingTimeForInstance(minter.ClientInstanceId); // Get the full duration for this minter.
                    TimeSpan timeSinceStart = DateTime.UtcNow - minter.LastCycleStartTimeUTC.Value;
                    float newTimeRemaining = totalCycleDuration - (float)timeSinceStart.TotalSeconds;

                    if (newTimeRemaining <= 0) // Cycle has completed
                    {
                        if (minter.State != MinterState.CycleCompleted) // Only trigger change if it wasn't already marked
                        {
                            minter.State = MinterState.CycleCompleted;
                            minter.TimeRemainingSeconds = 0;
                            minter.LastCycleStartTimeUTC = null; // Clear start time as the cycle is done
                            anyChangeMade = true;
                            _logger.LogInformation($"[ProcessInternal] Player {player.PlayerId}, Minter {minter.ClientInstanceId} cycle completed. NEW State: {minter.State}.");
                        }
                    }
                    else // Cycle is still in progress
                    {
                        // Check if the newly calculated time is different from the stored one.
                        // Use a small tolerance to avoid unnecessary database writes for tiny floating point differences.
                        if (Math.Abs(minter.TimeRemainingSeconds - newTimeRemaining) > 1.0f)
                        {
                            minter.TimeRemainingSeconds = newTimeRemaining;
                            anyChangeMade = true;
                            _logger.LogInformation($"[ProcessInternal] Player {player.PlayerId}, Minter {minter.ClientInstanceId} timer updated. NewTime: {minter.TimeRemainingSeconds:F1}s.");
                        }
                    }
                    minter.UpdatedAt = DateTime.UtcNow;
                    // --- END REVISED ---
                }
            }

            // Batch rewards are now handled when client calls /collect endpoint
            if (anyChangeMade) player.MemeMintPlayerData.UpdatedAt = currentTimeUtc;
            return anyChangeMade;
        }

        private void InitializeDefaultMinterInstances(PlayerMemeMintPlayerData playerData)
        {
            if (playerData.MinterInstances == null) playerData.MinterInstances = new List<MinterInstance>();
            int targetInstanceCount = 3; // Default to 3 slots
            for (int i = 0; i < targetInstanceCount; i++)
            {
                int clientInstanceId = i + 1;
                if (!playerData.MinterInstances.Any(m => m.ClientInstanceId == clientInstanceId))
                {
                    playerData.MinterInstances.Add(new MinterInstance
                    {
                        ClientInstanceId = clientInstanceId,
                        IsUnlocked = (clientInstanceId == 1), // First is unlocked
                        State = MinterState.Idle,
                        TimeRemainingSeconds = 0f,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // PUT: api/players/{playerId}/gameState
        [HttpPut("{playerId:long}/gameState")]
        [Authorize] // Secure this endpoint
        public async Task<IActionResult> UpdateGameState(long playerId, [FromBody] GameStateDto gameStateDto)
        {
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (requestingPlayerId != playerId.ToString()) return Forbid();

            var player = await _context.Players
                .Include(p => p.PlayerState)
                .Include(p => p.PlayerSettings)
                .Include(p => p.PlayerChatInfo)
                .Include(p => p.PlayerAgeVerification)
                .Include(p => p.PlayerUpgrades)
                .Include(p => p.PlayerAchievements)
                .Include(p => p.PlayerStatistics)
                // MemeMintPlayerData and MinterInstances are NOT loaded here for update via this generic endpoint.
                // They are handled by specific MemeMint endpoints (start, collect) and server-side processing (GetGameState).
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null) return NotFound($"Player {playerId} not found.");
            if (player.PlayerState == null) player.PlayerState = new PlayerState(); // Ensure it exists
            if (player.PlayerSettings == null) player.PlayerSettings = new PlayerSettings();
            if (player.PlayerChatInfo == null) player.PlayerChatInfo = new PlayerChatInfo();
            if (player.PlayerAgeVerification == null) player.PlayerAgeVerification = new PlayerAgeVerification { AgeVerificationStatusId = 1 };

            // Update PlayerState
            var stateDto = gameStateDto.PlayerState;
            _logger.LogInformation($"[UpdateGameState] PlayerID: {playerId}. Received GoldBars from client: {stateDto.GoldBars}");
            player.PlayerState.GoldBars = stateDto.GoldBars; 
            player.PlayerState.CurrentScore = stateDto.CurrentScore;
            player.PlayerState.TotalLifeTimeScoreEarned = stateDto.TotalLifeTimeScoreEarned;
            player.PlayerState.PrestigeCount = stateDto.PrestigeCount;
            player.PlayerState.StoredOfflineTimeSeconds = stateDto.StoredOfflineTimeSeconds;
            player.PlayerState.MaxOfflineStorageHours = stateDto.MaxOfflineStorageHours;
            player.PlayerState.TimePerClickSecond = stateDto.TimePerClickSecond;
            player.PlayerState.UpdatedAt = DateTime.UtcNow;
            // LastSaveTimestamp is critical for offline calculations. It's set here.
            player.PlayerState.LastSaveTimestamp = DateTime.UtcNow; 

            // Update PlayerSettings
            var settingsDto = gameStateDto.PlayerSettings;
            player.PlayerSettings.MusicVolume = settingsDto.MusicVolume;
            player.PlayerSettings.SfxVolume = settingsDto.SfxVolume;
            player.PlayerSettings.NotificationsEnabled = settingsDto.NotificationsEnabled;
            player.PlayerSettings.UpdatedAt = DateTime.UtcNow;

            // Update PlayerUpgrades (example: replace all with client's list)
            _context.PlayerUpgrades.RemoveRange(player.PlayerUpgrades); // EF Core handles tracking
            player.PlayerUpgrades.Clear(); // Clear navigation property
            foreach (var upgradeDto in gameStateDto.PlayerUpgrades) 
            { 
                player.PlayerUpgrades.Add(new PlayerUpgrade 
                { 
                    PlayerId = playerId, 
                    UpgradeId = upgradeDto.UpgradeId, 
                    Level = upgradeDto.Level, 
                    PurchasedAt = DateTime.UtcNow, // Assuming new/updated upgrades are "purchased" now
                    LastLeveledAt = DateTime.UtcNow 
                }); 
            }

            // Update PlayerAchievements (example: replace all)
            _context.PlayerAchievements.RemoveRange(player.PlayerAchievements);
            player.PlayerAchievements.Clear();
            foreach (var achievementDto in gameStateDto.PlayerAchievements) 
            { 
                player.PlayerAchievements.Add(new PlayerAchievement 
                { 
                    PlayerId = playerId, 
                    AchievementId = achievementDto.AchievementId, 
                    UnlockedAt = achievementDto.UnlockedAt, 
                    RewardClaimed = false // Assuming DTO doesn't send this, or server sets it
                }); 
            }

            // Update PlayerStatistics (example: replace all)
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

            // --- MemeMintData and MinterInstances are NO LONGER updated through this general endpoint ---
            // All MemeMintData and MinterInstance changes are authoritatively handled by:
            // 1. GET /gameState: Server calculates offline progress and returns current authoritative state.
            // 2. POST /mememint/minters/{clientInstanceId}/start: Server initiates a minter cycle.
            // 3. POST /mememint/minters/{clientInstanceId}/collect: Server processes collection and grants rewards/progress.
            // The client should not send MemeMintData or MinterInstances in the UpdateGameState request payload.
            // If player.MemeMintPlayerData needs initialization, it happens during GET /gameState.
            _logger.LogInformation($"[UpdateGameState] PlayerID: {playerId}. Any MemeMintData in the DTO is ignored by this endpoint.");

            // Update LastLoginAt for the player
            player.LastLoginAt = DateTime.UtcNow;
            
            // Detach PlayerChatInfo if it was loaded to prevent unintended updates if its DTO part wasn't handled
            // (or handle its update explicitly if gameStateDto can change it)
            if (player.PlayerChatInfo != null && _context.Entry(player.PlayerChatInfo).State != EntityState.Detached) 
            {
                _context.Entry(player.PlayerChatInfo).State = EntityState.Detached;
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[UpdateGameState] PlayerID: {playerId} general state save completed.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, $"[UpdateGameState] Concurrency conflict for PlayerID {playerId}.");
                // Handle concurrency issues, e.g., by reloading or informing client.
                return Conflict("Data has been modified by another process. Please refresh and try again.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"[UpdateGameState] Database update error for PlayerID {playerId}.");
                return StatusCode(500, "An error occurred while saving game state.");
            }
            
            return NoContent();
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
            // --- ADD Authorization Check --- 
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation($"UpdateChatInfo AuthCheck: Comparing Token Sub='{requestingPlayerId ?? "NULL"}' with URL PlayerId='{playerId}' (as string: '{playerId.ToString()}')");
            if (requestingPlayerId != playerId.ToString())
            {
                _logger.LogWarning($"UpdateChatInfo AuthCheck FAILED. Token Sub '{requestingPlayerId ?? "NULL"}' != URL PlayerId '{playerId}'. Returning Forbid.");
                return Forbid();
            }
            _logger.LogInformation("UpdateChatInfo AuthCheck PASSED.");
            // ----------------------------------

            if (string.IsNullOrWhiteSpace(requestDto.ChatUsername))
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
        public async Task<ActionResult<PlayerAgeVerificationDto>> VerifyPlayerAge(long playerId, [FromBody] VerifyPlayerAgeRequestDto requestDto) // Added requestDto
        {
            // --- ADD Authorization Check --- 
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            _logger.LogInformation($"VerifyAge AuthCheck: Comparing Token Sub='{requestingPlayerId ?? "NULL"}' with URL PlayerId='{playerId}' (as string: '{playerId.ToString()}')");
            if (requestingPlayerId != playerId.ToString())
            {
                _logger.LogWarning($"VerifyAge AuthCheck FAILED. Token Sub '{requestingPlayerId ?? "NULL"}' != URL PlayerId '{playerId}'. Returning Forbid.");
                return Forbid();
            }
            _logger.LogInformation("VerifyAge AuthCheck PASSED.");
            // ----------------------------------

            // Validate DateOfBirth
            if (requestDto.DateOfBirth == default(DateTime) || requestDto.DateOfBirth > DateTime.UtcNow)
            {
                _logger.LogWarning($"[VerifyPlayerAge] Invalid DateOfBirth provided for PlayerID {playerId}: {requestDto.DateOfBirth}");
                return BadRequest("Invalid Date of Birth provided.");
            }

            var player = await _context.Players
                .Include(p => p.PlayerAgeVerification)
                .ThenInclude(pav => pav.AgeVerificationStatus)
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null)
            {
                // This check is technically redundant if PlayerExists was called before, but good for safety.
                if (!await PlayerExists(playerId)) return NotFound($"Player {playerId} not found."); 
                // If player exists but the include failed to load PlayerAgeVerification (should not happen if correctly initialized)
                _logger.LogError($"[VerifyPlayerAge] Player {playerId} found, but PlayerAgeVerification navigation property is unexpectedly null.");
                return StatusCode(500, $"Player data integrity issue for player {playerId}.");
            }

            // Ensure PlayerAgeVerification entity exists (it should have been created during player identification)
            if (player.PlayerAgeVerification == null)
            {
                _logger.LogWarning($"[VerifyPlayerAge] PlayerAgeVerification is null for PlayerID {playerId}. Initializing a new one.");
                player.PlayerAgeVerification = new PlayerAgeVerification { PlayerId = playerId, AgeVerificationStatusId = 1 }; // Default to Not Verified
                _context.PlayerAgeVerifications.Add(player.PlayerAgeVerification); // Explicitly add if creating new
            }

            // Find the ID for the "Verified" status (assuming it exists from seeding)
            // This status now means "DoB submitted and client-side platform minimum age met"
            long verifiedStatusId = await _context.AgeVerificationStatuses
                                            .Where(s => s.Status == "Verified")
                                            .Select(s => s.Id)
                                            .FirstOrDefaultAsync();

            if (verifiedStatusId == 0) {
                _logger.LogError($"ERROR verifying age for player {playerId}: 'Verified' status not found in DB.");
                return StatusCode(500, "Server configuration error: Cannot find 'Verified' status.");
            }

            player.PlayerAgeVerification.AgeVerificationStatusId = verifiedStatusId;
            player.PlayerAgeVerification.VerifiedAt = DateTime.UtcNow;
            player.PlayerAgeVerification.DateOfBirth = requestDto.DateOfBirth; // Save the Date of Birth
            player.PlayerAgeVerification.VerificationMethod = "ClientDOBInput"; // Indicate method
            player.PlayerAgeVerification.LastVerificationAttempt = DateTime.UtcNow;
            player.PlayerAgeVerification.VerificationAttemptCount = player.PlayerAgeVerification.VerificationAttemptCount + 1;

            try
            {
                await _context.SaveChangesAsync();
                 _logger.LogInformation($"[VerifyPlayerAge] PlayerID {playerId} successfully saved DateOfBirth: {requestDto.DateOfBirth:yyyy-MM-dd} and set status to Verified.");
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, $"ERROR saving age verification for player {playerId} with DOB {requestDto.DateOfBirth:yyyy-MM-dd}.");
                 return StatusCode(500, "An error occurred saving age verification.");
            }
            
            // After saving, fetch the status text if it wasn't loaded or to be safe
            var status = player.PlayerAgeVerification.AgeVerificationStatus?.Status ?? 
             await _context.AgeVerificationStatuses
                           .Where(s => s.Id == player.PlayerAgeVerification.AgeVerificationStatusId)
                           .Select(s => s.Status)
                           .FirstOrDefaultAsync() ?? "Unknown";


            var responseDto = new PlayerAgeVerificationDto
            {
                IsVerified = status == "Verified",
                DateOfBirth = player.PlayerAgeVerification.DateOfBirth,
                IsOver18 = CalculateIsOver18(player.PlayerAgeVerification.DateOfBirth)
            };

            return Ok(responseDto);
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

        // Add this new helper method inside PlayersController class if it's missing
        // or ensure it's correctly defined from previous steps.
        public float GetBaseMintingTimeForInstance(int clientInstanceId)
        {
            // TODO: Fetch this from config or player's specific upgrade level for this minter
            // THIS MUST BE SERVER-AUTHORITATIVE LATER.
            _logger.LogInformation($"[PlayersController] GetBaseMintingTimeForInstance called for ClientInstanceId: {clientInstanceId}. Returning MINT_BASE_DURATION_SECONDS: {MINT_BASE_DURATION_SECONDS}f.");
            return MINT_BASE_DURATION_SECONDS; 
        }

        // POST: api/players/{playerId}/mememint/minters/{clientInstanceId}/start
        [HttpPost("{playerId:long}/mememint/minters/{clientInstanceId:int}/start")]
        [Authorize]
        public async Task<ActionResult<StartMinterResponseDto>> StartMinterCycle(long playerId, int clientInstanceId)
        {
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (requestingPlayerId != playerId.ToString())
            {
                _logger.LogWarning($"[StartMinterCycle] Auth FAILED for PlayerId {playerId}. Token Sub was '{requestingPlayerId ?? "NULL"}'.");
                return Forbid();
            }

            var player = await _context.Players
                .Include(p => p.PlayerState)
                .Include(p => p.MemeMintPlayerData)
                    .ThenInclude(mmpd => mmpd.MinterInstances)
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null || player.PlayerState == null)
            {
                return NotFound($"Player or PlayerState not found for PlayerId {playerId}.");
            }

            if (player.MemeMintPlayerData == null)
            {
                _logger.LogWarning($"[StartMinterCycle] Player {playerId} does not have MemeMintPlayerData initialized. Creating one.");
                // If MemeMintPlayerData is null, create it and its default minter instances
                player.MemeMintPlayerData = new PlayerMemeMintPlayerData { PlayerId = playerId, CreatedAt = DateTime.UtcNow };
                // Initialize default minters for the new PlayerMemeMintPlayerData
                int targetInstanceCount = 3; // Or get from config
                for (int i = 0; i < targetInstanceCount; i++)
                {
                    int newInstanceId = i + 1;
                    player.MemeMintPlayerData.MinterInstances.Add(new MinterInstance
                    {
                        ClientInstanceId = newInstanceId,
                        IsUnlocked = (newInstanceId == 1), // Only instance 1 is unlocked by default
                        State = MinterState.Idle,
                        TimeRemainingSeconds = 0f,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                // _context.PlayerMemeMintDatas.Add(player.MemeMintPlayerData); // EF Core tracks it via player navigation property
            }

            var minter = player.MemeMintPlayerData.MinterInstances
                               .FirstOrDefault(mi => mi.ClientInstanceId == clientInstanceId);

            if (minter == null)
            {
                // This case should be rare now if we auto-initialize above, but keep as a safeguard
                _logger.LogWarning($"[StartMinterCycle] Minter instance {clientInstanceId} not found for PlayerId {playerId} even after attempting init.");
                return NotFound($"Minter instance {clientInstanceId} not found.");
            }

            if (!minter.IsUnlocked)
            {
                _logger.LogWarning($"[StartMinterCycle] Minter instance {clientInstanceId} for PlayerId {playerId} is locked.");
                return BadRequest("Minter instance is locked.");
            }

            if (minter.State != MinterState.Idle)
            {
                _logger.LogWarning($"[StartMinterCycle] Minter instance {clientInstanceId} for PlayerId {playerId} is not Idle (Current: {minter.State}).");
                return Conflict("Minter instance is already processing or completed.");
            }

            decimal costPerMintActionGB = MINT_COST_GOLD_BARS; 

            if (!decimal.TryParse(player.PlayerState.GoldBars, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal currentGoldBarsOnServer))
            {
                _logger.LogError($"[StartMinterCycle] Failed to parse GoldBars for PlayerId {playerId} from PlayerState: {player.PlayerState.GoldBars}");
                currentGoldBarsOnServer = 0; 
            }

            if (currentGoldBarsOnServer < costPerMintActionGB)
            {
                _logger.LogWarning($"[StartMinterCycle] PlayerId {playerId} has insufficient GoldBars. Needs {costPerMintActionGB}, has {currentGoldBarsOnServer}.");
                return StatusCode(402, "Insufficient Gold Bars."); 
            }

            player.PlayerState.GoldBars = (currentGoldBarsOnServer - costPerMintActionGB).ToString(CultureInfo.InvariantCulture);
            player.PlayerState.UpdatedAt = DateTime.UtcNow;

            float baseMintingTime = GetBaseMintingTimeForInstance(clientInstanceId); 
            minter.State = MinterState.MintingInProgress;
            minter.TimeRemainingSeconds = baseMintingTime; 
            minter.LastCycleStartTimeUTC = DateTime.UtcNow;
            minter.UpdatedAt = DateTime.UtcNow;
            
            player.MemeMintPlayerData.UpdatedAt = DateTime.UtcNow; 

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"[StartMinterCycle] PlayerId {playerId}, Minter {clientInstanceId} started. Deducted {costPerMintActionGB} GB. New GB: {player.PlayerState.GoldBars}. Timer: {minter.TimeRemainingSeconds}s.");

                var responseDto = new StartMinterResponseDto
                {
                    UpdatedMinterInstance = new MinterInstanceDataDto
                    {
                        InstanceId = minter.ClientInstanceId,
                        State = (MinterStateDto)minter.State,
                        TimeRemainingSeconds = minter.TimeRemainingSeconds,
                        IsUnlocked = minter.IsUnlocked
                    },
                    NewGoldBarBalance = player.PlayerState.GoldBars,
                    ServerTimeUtc = minter.LastCycleStartTimeUTC ?? DateTime.UtcNow 
                };
                return Ok(responseDto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"[StartMinterCycle] DbUpdateException for PlayerId {playerId}, Minter {clientInstanceId}.");
                return StatusCode(500, "An error occurred while starting the minting cycle.");
            }
        }

        // POST: api/players/{playerId}/mememint/minters/{clientInstanceId}/collect
        [HttpPost("{playerId:long}/mememint/minters/{clientInstanceId:int}/collect")]
        [Authorize]
        public async Task<ActionResult<ProcessCyclesResponseDto>> CollectMinterCycle(long playerId, int clientInstanceId)
        {
            var requestingPlayerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (requestingPlayerId != playerId.ToString()) return Forbid();

            var player = await _context.Players
                .Include(p => p.PlayerState) 
                .Include(p => p.MemeMintPlayerData).ThenInclude(mmpd => mmpd!.MinterInstances)
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player?.MemeMintPlayerData == null) 
            {
                _logger.LogWarning($"[CollectMinterCycle] Player {playerId} or their MemeMintPlayerData not found.");
                return NotFound("Player or MemeMintData not found.");
            }

            var minter = player.MemeMintPlayerData.MinterInstances.FirstOrDefault(mi => mi.ClientInstanceId == clientInstanceId);
            if (minter == null) 
            {
                _logger.LogWarning($"[CollectMinterCycle] Minter {clientInstanceId} for player {playerId} not found.");
                return NotFound("Minter not found.");
            }

            // --- NEW: Check if minter is MintingInProgress but actually completed by server time ---
            if (minter.State == MinterState.MintingInProgress && minter.LastCycleStartTimeUTC.HasValue)
            {
                float elapsedOnServer = (float)(DateTime.UtcNow - minter.LastCycleStartTimeUTC.Value).TotalSeconds;
                // Check if it should have completed based on its original TimeRemainingSeconds at the start of the cycle
                // This assumes TimeRemainingSeconds was set to full duration when MintingInProgress began.
                // We need to fetch the original cycle duration (MINT_BASE_DURATION_SECONDS for now)
                float originalCycleDuration = GetBaseMintingTimeForInstance(minter.ClientInstanceId); // Use helper

                if (elapsedOnServer >= originalCycleDuration) 
                {
                    _logger.LogInformation($"[CollectMinterCycle] Minter {clientInstanceId} for player {playerId} was MintingInProgress but server time indicates completion. Elapsed: {elapsedOnServer}s, OriginalDuration: {originalCycleDuration}s. Updating to CycleCompleted.");
                    minter.State = MinterState.CycleCompleted;
                    minter.TimeRemainingSeconds = 0; 
                    minter.LastCycleStartTimeUTC = null; 
                    minter.UpdatedAt = DateTime.UtcNow;
                    player.MemeMintPlayerData.UpdatedAt = DateTime.UtcNow; 
                }
            }
            // --- END NEW CHECK ---

            if (minter.State != MinterState.CycleCompleted)
            {
                _logger.LogWarning($"[CollectMinterCycle] Minter {clientInstanceId} for player {playerId} is not in CycleCompleted state (Current: {minter.State}). Cannot collect.");
                return BadRequest("Minter cycle not completed or already collected.");
            }

            player.MemeMintPlayerData.SharedMintProgress += MINT_PROGRESS_PER_CYCLE;
            minter.State = MinterState.Idle; 
            minter.TimeRemainingSeconds = 0;
            minter.LastCycleStartTimeUTC = null;
            minter.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation($"Player {playerId}, Minter {minter.ClientInstanceId} COLLECTED. SharedProgress now: {player.MemeMintPlayerData.SharedMintProgress}");

            if (player.MemeMintPlayerData.SharedMintProgress >= MINT_TOTAL_PROGRESS_FOR_BATCH)
            {
                int batchesCompleted = player.MemeMintPlayerData.SharedMintProgress / MINT_TOTAL_PROGRESS_FOR_BATCH;
                decimal pointsEarnedThisTime = batchesCompleted * MINT_GCM_POINTS_PER_BATCH;
                player.MemeMintPlayerData.PlayerGCMPMPoints += pointsEarnedThisTime;
                player.MemeMintPlayerData.SharedMintProgress %= MINT_TOTAL_PROGRESS_FOR_BATCH;
                _logger.LogInformation($"Player {playerId} BATCH REWARD from collect: Earned {pointsEarnedThisTime} GCM. New Total GCM: {player.MemeMintPlayerData.PlayerGCMPMPoints}. SharedProgress now: {player.MemeMintPlayerData.SharedMintProgress}");
            }
            
            player.MemeMintPlayerData.UpdatedAt = DateTime.UtcNow;
            
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Player {playerId}, Minter {clientInstanceId} collection processed and saved.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"[CollectMinterCycle] DbUpdateException for PlayerId {playerId}, Minter {clientInstanceId} during collect.");
                return StatusCode(500, "An error occurred while collecting minter reward.");
            }

            return Ok(new ProcessCyclesResponseDto 
            {
                UpdatedMemeMintData = new MemeMintPlayerDataDto 
                {
                    PlayerGCMPMPoints = player.MemeMintPlayerData.PlayerGCMPMPoints,
                    SharedMintProgress = player.MemeMintPlayerData.SharedMintProgress,
                    MinterInstances = player.MemeMintPlayerData.MinterInstances.Select(mi => new MinterInstanceDataDto
                    {
                        InstanceId = mi.ClientInstanceId,
                        State = (MinterStateDto)mi.State,
                        TimeRemainingSeconds = mi.TimeRemainingSeconds,
                        IsUnlocked = mi.IsUnlocked
                    }).ToList()
                }
            });
        }

        // Helper method to calculate if user is over 18
        private bool CalculateIsOver18(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue)
            {
                return false;
            }
            DateTime today = DateTime.UtcNow.Date;
            DateTime eighteenYearsAgo = today.AddYears(-18);
            return dateOfBirth.Value.Date <= eighteenYearsAgo;
        }

    }
}
