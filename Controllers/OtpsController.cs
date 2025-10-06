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
    public class OtpsController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public OtpsController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Otp>>> GetOtps()
        {
            return await _context.Otps.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Otp>> GetOtp(Guid id)
        {
            var otp = await _context.Otps.FindAsync(id);

            if (otp == null)
            {
                return NotFound();
            }

            return otp;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutOtp(Guid id, Otp otp)
        {
            if (id != otp.Id)
            {
                return BadRequest();
            }

            _context.Entry(otp).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OtpExists(id))
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
        public async Task<ActionResult<Otp>> PostOtp(Otp otp)
        {
            _context.Otps.Add(otp);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOtp", new { id = otp.Id }, otp);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOtp(Guid id)
        {
            var otp = await _context.Otps.FindAsync(id);
            if (otp == null)
            {
                return NotFound();
            }

            _context.Otps.Remove(otp);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OtpExists(Guid id)
        {
            return _context.Otps.Any(e => e.Id == id);
        }
    }
}
