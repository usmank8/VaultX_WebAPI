using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "resident")]
    public class ProfileController : ControllerBase
    {
        private readonly VaultxDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(VaultxDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID not found." });
            }

            var user = await _context.Users
                .Include(u => u.Residences)
                .FirstOrDefaultAsync(u => u.Userid == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var primaryResidence = user.Residences?.FirstOrDefault(r => r.IsPrimary);

            if (primaryResidence == null)
            {
                return NotFound(new { message = "No residence found for this user." });
            }

            return Ok(new
            {
                userid = user.Userid,
                firstname = user.Firstname,
                lastname = user.Lastname,
                Phone = user.Phone,
                cnic = user.Cnic,
                email = user.Email,
                isApprovedBySociety = primaryResidence.IsApprovedBySociety,  // ✅ Add this
                residence = new
                {
                    addressLine1 = primaryResidence.AddressLine1,
                    block = primaryResidence.Block,
                    residence = primaryResidence.Residence1,
                    residenceType = primaryResidence.ResidenceType,
                    isApprovedBySociety = primaryResidence.IsApprovedBySociety  // ✅ Also here
                }
            });
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUserProfile([FromBody] CreateProfileDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }

            var user = await _context.Users
                .Include(u => u.Residences)
                .FirstOrDefaultAsync(u => u.Userid == userId);

            if (user == null)
            {
                return Conflict("User not found.");
            }

            
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => (u.Cnic == dto.Cnic || u.Phone == dto.Phonenumber) && u.Userid != userId);

            if (existingUser != null)
            {
                bool cnicConflict = !string.IsNullOrEmpty(dto.Cnic) && existingUser.Cnic == dto.Cnic;
                bool phoneConflict = !string.IsNullOrEmpty(dto.Phonenumber) && existingUser.Phone == dto.Phonenumber;
                if (cnicConflict || phoneConflict)
                {
                    return Conflict("Another user with this CNIC or phone number already exists.");
                }
            }

            
            user.Firstname = dto.Firstname;
            user.Lastname = dto.Lastname;
            user.Phone = dto.Phonenumber;
            user.Cnic = dto.Cnic;

            
            var residence = new Residence
            {
                AddressLine1 = dto.Address ?? string.Empty,
                Block = dto.Block ?? string.Empty,
                Residence1 = dto.Residence.ToString().ToLower() ?? string.Empty,
                ResidenceType = dto.ResidenceType.ToString().ToLower() ?? string.Empty,
                IsPrimary = true,
                User = user
            };

            user.Residences = user.Residences ?? new List<Residence>();
            user.Residences.Add(residence);

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile.");
                return StatusCode(500, "Failed to update user profile.");
            }
        }

        [HttpPost("password/update")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdatePasswordDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Userid == userId);

            if (user == null)
            {
                return Conflict("User not found.");
            }

            
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password))
            {
                return Conflict("Old password is incorrect.");
            }

            
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user password.");
                return StatusCode(500, "Failed to update user password.");
            }
        }

        [HttpPatch("update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Userid == userId);

                if (user == null)
                {
                    return Conflict("User not found.");
                }

                
                if (!string.IsNullOrEmpty(dto.Cnic) || !string.IsNullOrEmpty(dto.Phonenumber))
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Userid != userId &&
                            ((dto.Cnic != null && u.Cnic == dto.Cnic) ||
                             (dto.Phonenumber != null && u.Phone == dto.Phonenumber)));

                    if (existingUser != null)
                    {
                        return Conflict("Another user with this CNIC or phone number already exists.");
                    }
                }

                
                if (dto.Firstname != null) user.Firstname = dto.Firstname;
                if (dto.Lastname != null) user.Lastname = dto.Lastname;
                if (dto.Phonenumber != null) user.Phone = dto.Phonenumber;
                if (dto.Cnic != null) user.Cnic = dto.Cnic;

                await _context.SaveChangesAsync();

                
                var residence = await _context.Residences
                    .FirstOrDefaultAsync(r => r.Userid == userId && r.IsPrimary);

                if (residence == null)
                {
                    return Conflict("Primary residence not found for user.");
                }

                if (dto.Residence.HasValue) residence.Residence1 = dto.Residence.Value.ToString().ToLower();
                if (dto.ResidenceType.HasValue) residence.ResidenceType = dto.ResidenceType.Value.ToString().ToLower();
                if (dto.Block != null) residence.Block = dto.Block;
                if (dto.Address != null) residence.AddressLine1 = dto.Address;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating profile.");
                return StatusCode(500, "Failed to update profile.");
            }
        }

        [HttpGet("residences")]
        public async Task<IActionResult> GetMyResidences()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var residences = await _context.Residences
                .Include(r => r.Vehicles)  // ← ADD THIS to avoid extra query
                .Where(r => r.Userid == userId)
                .ToListAsync();  // ← Get list first

            var result = residences.Select(r => new
            {
                r.Id,
                Residence = r.Residence1,
                r.ResidenceType,
                r.Block,
                Address = r.AddressLine1,  // ← CHANGED from r.Address
                r.IsPrimary,
                VehicleCount = r.Vehicles.Count,
                r.IsApprovedBySociety
            }).ToList();

            return Ok(result);
        }

        [HttpPost("add-residence")]
        public async Task<IActionResult> AddResidence([FromBody] AddResidenceDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsVerified != true)
                return BadRequest(new { Message = "Your account is not approved yet." });

            var residence = new Residence
            {
                Id = Guid.NewGuid(),
                Userid = userId,
                Residence1 = dto.Residence,
                ResidenceType = dto.ResidenceType,
                Block = dto.Block,
                AddressLine1 = dto.Address,  // ← CHANGED from Address = dto.Address
                IsPrimary = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Residences.Add(residence);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Residence added successfully.", ResidenceId = residence.Id });
        }

        /// <summary>
        /// Switch primary residence
        /// </summary>
        [HttpPatch("residences/{id}/set-primary")]
        public async Task<IActionResult> SetPrimaryResidence(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Verify residence belongs to user
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == id && r.Userid == userId);

            if (residence == null)
                return NotFound(new { Message = "Residence not found." });

            // Set all other residences to non-primary
            var allResidences = await _context.Residences
                .Where(r => r.Userid == userId)
                .ToListAsync();

            foreach (var r in allResidences)
            {
                r.IsPrimary = (r.Id == id);
                r.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Primary residence updated successfully." });
        }

        /// <summary>
        /// Delete a residence (cannot delete primary)
        /// </summary>
        [HttpDelete("residences/{id}")]
        public async Task<IActionResult> DeleteResidence(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var residence = await _context.Residences
                .Include(r => r.Guests)
                .Include(r => r.Vehicles)
                .FirstOrDefaultAsync(r => r.Id == id && r.Userid == userId);

            if (residence == null)
                return NotFound(new { Message = "Residence not found." });

            if (residence.IsPrimary)
                return BadRequest(new { Message = "Cannot delete primary residence." });

            // Check if residence has guests or vehicles
            if (residence.Guests.Any() || residence.Vehicles.Any())
            {
                return BadRequest(new 
                { 
                    Message = "Cannot delete residence with associated guests or vehicles.",
                    GuestCount = residence.Guests.Count,
                    VehicleCount = residence.Vehicles.Count
                });
            }

            _context.Residences.Remove(residence);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Residence deleted successfully." });
        }

        /// <summary>
        /// Get detailed info for a specific residence
        /// </summary>
        [HttpGet("residences/{id}")]
        public async Task<IActionResult> GetResidenceDetails(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var residence = await _context.Residences
                .Include(r => r.Guests)
                .Include(r => r.Vehicles)
                .FirstOrDefaultAsync(r => r.Id == id && r.Userid == userId);

            if (residence == null)
                return NotFound(new { Message = "Residence not found." });

            return Ok(new
            {
                id = residence.Id,
                residence = residence.Residence1,
                residenceType = residence.ResidenceType,
                block = residence.Block,
                address = residence.AddressLine1,
                isPrimary = residence.IsPrimary,
                isApprovedBySociety = residence.IsApprovedBySociety,
                guestCount = residence.Guests.Count,
                vehicleCount = residence.Vehicles.Count,
                createdAt = residence.CreatedAt,
                updatedAt = residence.UpdatedAt
            });
        }
    }
}
