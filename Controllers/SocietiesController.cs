using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SocietiesController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public SocietiesController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Society>>> GetSocieties()
        {
            return await _context.Societies.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Society>> GetSociety(string id)
        {
            var society = await _context.Societies.FindAsync(id);

            if (society == null)
            {
                return NotFound();
            }

            return society;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutSociety(string id, Society society)
        {
            if (id != society.SocietyId)
            {
                return BadRequest();
            }

            _context.Entry(society).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SocietyExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Society>> PostSociety(Society society)
        {
            _context.Societies.Add(society);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (SocietyExists(society.SocietyId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetSociety", new { id = society.SocietyId }, society);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSociety(string id)
        {
            var society = await _context.Societies.FindAsync(id);
            if (society == null)
            {
                return NotFound();
            }

            _context.Societies.Remove(society);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SocietyExists(string id)
        {
            return _context.Societies.Any(e => e.SocietyId == id);
        }
    }
}
