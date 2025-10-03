using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResidencesController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public ResidencesController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Residence>>> GetResidences()
        {
            return await _context.Residences.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Residence>> GetResidence(Guid id)
        {
            var residence = await _context.Residences.FindAsync(id);

            if (residence == null)
            {
                return NotFound();
            }

            return residence;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutResidence(Guid id, Residence residence)
        {
            if (id != residence.Id)
            {
                return BadRequest();
            }

            _context.Entry(residence).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResidenceExists(id))
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
        public async Task<ActionResult<Residence>> PostResidence(Residence residence)
        {
            _context.Residences.Add(residence);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetResidence", new { id = residence.Id }, residence);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResidence(Guid id)
        {
            var residence = await _context.Residences.FindAsync(id);
            if (residence == null)
            {
                return NotFound();
            }

            _context.Residences.Remove(residence);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ResidenceExists(Guid id)
        {
            return _context.Residences.Any(e => e.Id == id);
        }
    }
}
