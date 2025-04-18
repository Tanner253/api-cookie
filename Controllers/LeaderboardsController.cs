using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data.Context;
using Api.Data.Dtos;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Numerics; // Needed for BigInteger parsing

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
                var playersQuery = _context.Players
                    .Include(p => p.PlayerState)
                    .Include(p => p.PlayerChatInfo)
                    .Where(p => p.PlayerState != null && p.PlayerChatInfo != null && !string.IsNullOrEmpty(p.PlayerChatInfo.ChatUsername));

                // Materialize the necessary data to perform client-side sorting
                // because EF Core might struggle translating BigInteger parsing/sorting.
                var playersData = await playersQuery
                    .Select(p => new
                    {
                        PlayerId = p.PlayerId,
                        Username = p.PlayerChatInfo.ChatUsername,
                        TotalScoreString = p.PlayerState.TotalLifeTimeScoreEarned ?? "0", // Use ?? "0" for safety
                        PrestigeCount = p.PlayerState.PrestigeCount
                    })
                    .ToListAsync(); // Bring data into memory

                // Log fetched data BEFORE sorting/parsing
                _logger.LogInformation("--- Fetched Player Data Before Sort ---");
                foreach (var pData in playersData)
                {
                     _logger.LogInformation($"Fetched: PlayerId={pData.PlayerId}, User={pData.Username}, ScoreStr={pData.TotalScoreString ?? "NULL"}, Prestige={pData.PrestigeCount}");
                }
                 _logger.LogInformation("-------------------------------------");

                // Parse score for secondary sorting, Sort primarily by Prestige
                var sortedPlayers = playersData
                    .Select(p => new {
                        p.PlayerId,
                        p.Username,
                        p.TotalScoreString, // Keep original string
                        TotalScoreNumeric = BigInteger.TryParse(p.TotalScoreString, out var score) ? score : BigInteger.Zero, // Parse for sorting
                        p.PrestigeCount
                    })
                    .OrderByDescending(p => p.PrestigeCount) // Primary sort: Prestige (desc)
                    .ThenByDescending(p => p.TotalScoreNumeric) // Secondary sort: Parsed Score (desc)
                    .Take(50) // Take top 50
                    .ToList();

                // Add rank and log the final DTO data being sent
                _logger.LogInformation("--- Final Leaderboard DTO Data ---");
                var leaderboard = sortedPlayers
                    .Select((p, index) => {
                         var dto = new LeaderboardEntryDto
                         {
                             Rank = index + 1,
                             Username = p.Username ?? "Unknown",
                             TotalLifetimeScore = p.TotalScoreString, // Use the ORIGINAL string from DB
                             PrestigeCount = p.PrestigeCount
                         };
                         _logger.LogInformation($"DTO: Rank={dto.Rank}, User={dto.Username}, ScoreString='{dto.TotalLifetimeScore}', Prestige={dto.PrestigeCount}"); // Log ScoreString
                         return dto;
                     })
                    .ToList();
                _logger.LogInformation("--------------------------------");

                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top players leaderboard.");
                return StatusCode(500, "An internal error occurred while fetching the leaderboard.");
            }
        }
    }
} 