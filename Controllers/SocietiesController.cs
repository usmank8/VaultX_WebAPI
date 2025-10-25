using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SocietyController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public SocietyController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AddSociety([FromBody] CreateSocietyDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Userid == userId && u.Role == "admin");

            if (adminUser == null)
            {
                return NotFound("Admin user not found or unauthorized.");
            }

            var existingSociety = await _context.Societies
                .FirstOrDefaultAsync(s => s.User.Userid == userId);

            if (existingSociety != null)
            {
                return Conflict("A society linked to this admin user already exists.");
            }

            var newSociety = new Society
            {
                Name = dto.Name,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                User = adminUser
            };

            try
            {
                _context.Societies.Add(newSociety);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving society: {ex.Message}");
                return StatusCode(500, "Failed to create society.");
            }
        }

        [HttpGet("mine")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetMySociety()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            Console.WriteLine($"Fetching society for admin ID: {userId}");
            var societies = await _context.Societies
                .Where(s => s.User.Userid == userId)
                .ToListAsync();

            if (!societies.Any())
            {
                return NotFound("Society not found for this admin user.");
            }

            var society = societies[0];

            var dto = new SocietyDto
            {
                SocietyId = society.SocietyId,
                Name = society.Name,
                Address = society.Address,
                City = society.City,
                State = society.State,
                PostalCode = society.PostalCode
            };

            return Ok(dto);
        }

        [HttpPut("update")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateSociety([FromBody] UpdateSocietyDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var society = await _context.Societies
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.User.Userid == userId);

            if (society == null)
            {
                return NotFound("Society not found for this user.");
            }

            if (dto.Name != null) society.Name = dto.Name;
            if (dto.Address != null) society.Address = dto.Address;
            if (dto.City != null) society.City = dto.City;
            if (dto.State != null) society.State = dto.State;
            if (dto.PostalCode != null) society.PostalCode = dto.PostalCode;

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating society: {ex.Message}");
                return StatusCode(500, "Failed to update society");
            }
        }
    }
}
