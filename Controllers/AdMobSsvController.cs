using Api.Data.Context;
using Api.Data.Dtos;
using Api.Data.Models; // Required for AdMobSsvTransaction
using Api.Services;    // Required for IAdMobSsvVerifierService
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for AnyAsync, FirstOrDefaultAsync
using Microsoft.Extensions.Logging;
using System;
using System.Linq; // Required for Linq operations like .Any()
using System.Net;  // Required for WebUtility
using System.Threading.Tasks;
using System.Globalization; // For CultureInfo.InvariantCulture

namespace Api.Controllers
{
    [ApiController]
    [Route("api/admob")]
    public class AdMobSsvController : ControllerBase
    {
        private readonly ILogger<AdMobSsvController> _logger;
        private readonly IAdMobSsvVerifierService _ssvVerifier;
        private readonly AppDbContext _dbContext;

        public AdMobSsvController(
            ILogger<AdMobSsvController> logger, 
            AppDbContext dbContext, 
            IAdMobSsvVerifierService ssvVerifier) // Added verifier service
        {
            _logger = logger;
            _dbContext = dbContext;
            _ssvVerifier = ssvVerifier;
        }

        [HttpGet("ssv-callback")]
        public async Task<IActionResult> VerifySsvCallback([FromQuery] AdMobSsvCallbackDto callbackData)
        {
            _logger.LogInformation("Received AdMob SSV Callback: {@CallbackData}", callbackData);

            // 1. Get raw query string (excluding the '?')
            string rawQueryString = HttpContext.Request.QueryString.Value ?? string.Empty;
            if (rawQueryString.StartsWith("?"))
            {
                rawQueryString = rawQueryString.Substring(1);
            }

            // 2. Verify the callback
            bool isValid = await _ssvVerifier.VerifyCallbackAsync(rawQueryString, callbackData.Signature, callbackData.KeyId);

            if (!isValid)
            {
                _logger.LogWarning("AdMob SSV callback verification failed for TransactionId: {TransactionId}. Raw Query: {RawQuery}", callbackData.TransactionId, rawQueryString);
                return BadRequest("Signature verification failed."); 
            }

            _logger.LogInformation("AdMob SSV callback VERIFIED for TransactionId: {TransactionId}", callbackData.TransactionId);

            // UserId is optional. RewardAmount, RewardItem, and TransactionId are critical.
            if (string.IsNullOrEmpty(callbackData.TransactionId) || 
                string.IsNullOrEmpty(callbackData.RewardAmount) || 
                string.IsNullOrEmpty(callbackData.RewardItem))
            {
                _logger.LogError("Critical SSV parameters (TransactionId, RewardAmount, RewardItem) are missing or empty after successful verification. CallbackData: {@CallbackData}", callbackData);
                return BadRequest("Missing critical reward information (TransactionId, RewardAmount, RewardItem) after verification.");
            }
            
            if (await _dbContext.AdMobSsvTransactions.AnyAsync(t => t.TransactionId == callbackData.TransactionId))
            {
                _logger.LogWarning("Duplicate AdMob SSV TransactionId received: {TransactionId}", callbackData.TransactionId);
                return Ok("Transaction already processed."); 
            }

            long? parsedPlayerId = null; // Nullable long for PlayerId
            if (!string.IsNullOrEmpty(callbackData.UserId))
            {
                if (long.TryParse(callbackData.UserId, out long tempPlayerId))
                {
                    parsedPlayerId = tempPlayerId;
                }
                else
                {
                    _logger.LogWarning("Could not parse UserId '{UserVal}' to long. Proceeding without PlayerId for TransactionId: {TransactionId}", callbackData.UserId, callbackData.TransactionId);
                    // For a real ad, if UserId is present but malformed, you might choose to BadRequest.
                    // For AdMob's "Verify URL" test, it might send user_id as an empty string, which is fine.
                }
            }
            else
            {
                 _logger.LogInformation("UserId is null or empty in SSV callback for TransactionId: {TransactionId}. This is acceptable.", callbackData.TransactionId);
            }
            
            string customDataDecoded = string.IsNullOrEmpty(callbackData.CustomData) 
                ? string.Empty 
                : WebUtility.UrlDecode(callbackData.CustomData);

            if(!decimal.TryParse(callbackData.RewardAmount, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rewardAmountValue))
            {
                _logger.LogError("Failed to parse RewardAmount from AdMob callback: {RewardAmount} for TransactionId: {TransactionId}", callbackData.RewardAmount, callbackData.TransactionId);
                return BadRequest("Invalid reward_amount.");
            }
            string rewardItemName = callbackData.RewardItem;

            DateTime adCompletionTimestamp;
            if (long.TryParse(callbackData.Timestamp, out long timestampMs))
            {
                adCompletionTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime;
            }
            else
            {
                _logger.LogWarning("Failed to parse Timestamp from AdMob callback: {Timestamp} for TransactionId: {TransactionId}. Using UtcNow.", callbackData.Timestamp, callbackData.TransactionId);
                adCompletionTimestamp = DateTime.UtcNow;
            }

            // 5. Store the transaction
            var newTransaction = new AdMobSsvTransaction
            {
                TransactionId = callbackData.TransactionId!,
                PlayerId = parsedPlayerId,
                RewardItem = rewardItemName!,
                RewardAmount = rewardAmountValue,
                AdCompletionTimestamp = adCompletionTimestamp,
                ProcessedAt = DateTime.UtcNow
            };
            _dbContext.AdMobSsvTransactions.Add(newTransaction);
            
            if (parsedPlayerId.HasValue)
            {
                var playerState = await _dbContext.PlayerStates.FirstOrDefaultAsync(ps => ps.PlayerId == parsedPlayerId.Value);
                if (playerState == null)
                {
                    _logger.LogError("PlayerState not found for PlayerId: {PlayerId} during SSV reward. Transaction will be saved, but reward not granted.", parsedPlayerId.Value);
                    await _dbContext.SaveChangesAsync(); // Save transaction even if player reward fails for audit
                    return NotFound($"PlayerState for PlayerId {parsedPlayerId.Value} not found. Reward not granted, but transaction logged.");
                }

                // Example: Granting "GoldBars"
                if (rewardItemName.Equals("GoldBars", StringComparison.OrdinalIgnoreCase)) 
                {
                    // Assuming playerState.GoldBars is also string, needs similar parsing if it's numeric reward
                    if (decimal.TryParse(playerState.GoldBars, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal currentGoldBars))
                    {
                        currentGoldBars += rewardAmountValue;
                        playerState.GoldBars = currentGoldBars.ToString(CultureInfo.InvariantCulture);
                        _logger.LogInformation("Granted {RewardAmount} {RewardItem} to PlayerId {PlayerId}. New GoldBars: {NewGoldBars}",
                            rewardAmountValue, rewardItemName, parsedPlayerId.Value, playerState.GoldBars);
                    }
                    else
                    {
                        _logger.LogError("Could not parse current GoldBars for PlayerId {PlayerId}. Value: {GoldBarsValue}", parsedPlayerId.Value, playerState.GoldBars);
                        // Decide if this is a critical failure for the transaction
                    }
                }
                // Example: Granting "Score"
                else if (rewardItemName.Equals("Score", StringComparison.OrdinalIgnoreCase)) 
                {
                    if (decimal.TryParse(playerState.CurrentScore, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal currentScoreValue))
                    {
                        currentScoreValue += rewardAmountValue;
                        playerState.CurrentScore = currentScoreValue.ToString(CultureInfo.InvariantCulture);
                        _logger.LogInformation("Granted {RewardAmount} {RewardItem} to PlayerId {PlayerId}. New CurrentScore: {NewScore}",
                            rewardAmountValue, rewardItemName, parsedPlayerId.Value, playerState.CurrentScore);
                    }
                    else
                    {
                         _logger.LogError("Could not parse CurrentScore for PlayerId {PlayerId}. Value: {CurrentScoreValue}", parsedPlayerId.Value, playerState.CurrentScore);
                        // Decide if this is a critical failure for the transaction
                    }
                }
                else
                {
                    _logger.LogWarning("Unhandled reward_item type '{RewardItem}' for PlayerId {PlayerId}. Reward not directly applied to PlayerState.", rewardItemName, parsedPlayerId.Value);
                }
            }
            else
            {
                _logger.LogInformation("No PlayerId provided with this SSV callback (TransactionId: {TransactionId}). Transaction logged, no specific player reward given.", callbackData.TransactionId);
            }
            
            // 7. SaveChangesAsync to database (includes transaction and player state changes)
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch(DbUpdateException ex)
            {
                _logger.LogError(ex, "Error saving changes to database during SSV callback for TransactionId: {TransactionId}", callbackData.TransactionId);
                // Consider what to return here. If the transaction was already saved due to player not found, this might be an issue.
                // For now, assume the first SaveChangesAsync is the primary one for the transaction record itself.
                return StatusCode(500, "Error saving reward information.");
            }

            _logger.LogInformation("Successfully processed AdMob SSV callback for TransactionId: {TransactionId}", callbackData.TransactionId);
            // AdMob expects an HTTP 200 OK response for successful callbacks
            return Ok("Callback processed."); // Changed message to be more generic for test pings
        }
    }
} 