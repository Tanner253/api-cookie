using Microsoft.AspNetCore.Mvc;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using Api.Data.Context;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerDataController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PlayerDataController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/playerdata/id
        [HttpGet("{id}")]
        public async Task<ActionResult<PlayerData>> GetPlayerDataById(int id)
        {
            var playerData = await _context.PlayerDatas.FindAsync(id);

            if (playerData == null)
            {
                return NotFound($"Player data {id} not found");
            }

            return Ok(playerData);
        }


        //POST - CREATE
        [HttpPost]
        public async Task<ActionResult<PlayerData>> CreatePlayerData([FromBody] PlayerData newPlayerData)
        {
            if (newPlayerData == null)
            {
                return BadRequest("Player data is null");
            }

            newPlayerData.LastSavedTimestampTicks = DateTime.UtcNow.Ticks;

            _context.PlayerDatas.Add(newPlayerData);

            await _context.SaveChangesAsync();


            //return a 201 created response with the new player data
            return CreatedAtAction(nameof(GetPlayerDataById), new { id = newPlayerData.Id }, newPlayerData);

        }


        //PUT - UPDATE
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePlayerData(int id, [FromBody] PlayerData playerData)
        {
            if (playerData == null)
            {
                return BadRequest("Player data is null");
            }

            if (playerData.Id != id)
            {
                return BadRequest("Player data ID mismatch");
            }

            //retrieve active record
            var existingPlayerData = await _context.PlayerDatas.FindAsync(id);
            if (existingPlayerData == null)
            {
                return NotFound($"Player not found with ID {id}");
            }

            //update existing record
            existingPlayerData.GoldNuggets = playerData.GoldNuggets;
            existingPlayerData.LastSavedTimestampTicks = DateTime.UtcNow.Ticks;

            //try to save
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await playerDataExists(id))
                {
                    return NotFound($"Player data {id} not found");
                }
                else
                {
                    throw;
                }

            }
            return NoContent();
        }
        private async Task<bool> playerDataExists(int id)
        {
            return await _context.PlayerDatas.AnyAsync(e => e.Id == id);
        }

    }
}
