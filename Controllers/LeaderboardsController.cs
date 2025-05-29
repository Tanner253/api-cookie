using System.Collections.Generic;
using System.Linq;
using System.Numerics; // Needed for BigInteger parsing
using System.Threading.Tasks;
using Api.Data.Context;
using Api.Data.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/leaderboards")]
    public class LeaderboardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LeaderboardsController> _logger;

        public LeaderboardsController(AppDbContext context, ILogger<LeaderboardsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/leaderboards/top
        [HttpGet("top")]
        public async Task<ActionResult<List<LeaderboardEntryDto>>> GetTopPlayers()
        {
            try
            {
                // Fetch players with state and chat info, filter those with usernames
                var playersQuery = _context
                    .Players.Include(p => p.PlayerState)
                    .Include(p => p.PlayerChatInfo)
                    .Where(p =>
                        p.PlayerState != null
                        && p.PlayerChatInfo != null
                        && !string.IsNullOrEmpty(p.PlayerChatInfo.ChatUsername)
                    );

                // Materialize the necessary data to perform client-side sorting
                // because EF Core might struggle translating BigInteger parsing/sorting.
                var playersData = await playersQuery
                    .Select(p => new
                    {
                        PlayerId = p.PlayerId,
                        Username = p.PlayerChatInfo!.ChatUsername,
                        TotalScoreString = p.PlayerState!.TotalLifeTimeScoreEarned ?? "0",
                        PrestigeCount = p.PlayerState!.PrestigeCount,
                    })
                    .ToListAsync(); // Bring data into memory

                // Log fetched data BEFORE sorting/parsing
                _logger.LogInformation("--- Fetched Player Data Before Sort ---");
                foreach (var pData in playersData)
                {
                    _logger.LogInformation(
                        $"Fetched: PlayerId={pData.PlayerId}, User={pData.Username}, ScoreStr={pData.TotalScoreString ?? "NULL"}, Prestige={pData.PrestigeCount}"
                    );
                }
                _logger.LogInformation("-------------------------------------");

                // Parse score for secondary sorting, Sort primarily by Prestige
                var sortedPlayers = playersData
                    .Select(p => new
                    {
                        p.PlayerId,
                        p.Username,
                        p.TotalScoreString, // Keep original string
                        TotalScoreNumeric = ParseScoreStringHelper(p.TotalScoreString),
                        p.PrestigeCount,
                    })
                    .OrderByDescending(p => p.PrestigeCount) // Primary sort: Prestige (desc)
                    .ThenByDescending(p => p.TotalScoreNumeric) // Secondary sort: Parsed Score (desc)
                    .Take(50) // Take top 50
                    .ToList();

                // Add rank and log the final DTO data being sent
                _logger.LogInformation("--- Final Leaderboard DTO Data ---");
                var leaderboard = sortedPlayers
                    .Select(
                        (p, index) =>
                        {
                            var dto = new LeaderboardEntryDto
                            {
                                Rank = index + 1,
                                Username = p.Username ?? "Unknown",
                                TotalLifetimeScore = p.TotalScoreString, // Use the ORIGINAL string from DB
                                PrestigeCount = p.PrestigeCount,
                            };
                            _logger.LogInformation(
                                $"DTO: Rank={dto.Rank}, User={dto.Username}, ScoreString='{dto.TotalLifetimeScore}', Prestige={dto.PrestigeCount}"
                            ); // Log ScoreString
                            return dto;
                        }
                    )
                    .ToList();
                _logger.LogInformation("--------------------------------");

                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top players leaderboard.");
                return StatusCode(
                    500,
                    "An internal error occurred while fetching the leaderboard."
                );
            }
        }

        private static decimal ParseScoreStringHelper(string scoreString)
        {
            if (string.IsNullOrWhiteSpace(scoreString))
            {
                return 0M;
            }

            scoreString = scoreString.Trim();
            char lastChar = scoreString.Last();
            string numericPart = scoreString;
            decimal multiplier = 1M;

            // Define suffixes and their multipliers
            // Using a case-insensitive approach for the suffix character
            var suffixMultipliers = new Dictionary<char, decimal>()
            {
                { 'K', 1_000M },
                { 'M', 1_000_000M },
                { 'B', 1_000_000_000M },
                { 'T', 1_000_000_000_000M },
                // Add 'Q' for Quadrillion, 'Qa' for Quintillion etc. if your scores can reach that
                // For 'Qa' or other multi-character suffixes, this simple char-based lookup would need adjustment
            };

            if (char.IsLetter(lastChar))
            {
                char upperSuffix = char.ToUpperInvariant(lastChar);
                if (suffixMultipliers.TryGetValue(upperSuffix, out decimal foundMultiplier))
                {
                    multiplier = foundMultiplier;
                    numericPart = scoreString.Substring(0, scoreString.Length - 1);
                }
                // If it's a letter but not a recognized suffix, it will try to parse numericPart (which is the full string)
                // and will likely fail if the letter is not part of a valid number format, resulting in 0M below.
            }

            if (
                decimal.TryParse(
                    numericPart,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out decimal baseValue
                )
            )
            {
                return baseValue * multiplier;
            }

            // Optional: Log a warning if parsing fails for an unexpected format
            // _logger.LogWarning($"Failed to parse score string: {scoreString}. Numeric part attempted: {numericPart}");
            return 0M; // Default to 0 if parsing fails
        }
    }
}
