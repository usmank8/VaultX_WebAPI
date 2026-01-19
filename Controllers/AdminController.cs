using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace VaultX_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,employee")]
    public class AdminController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public AdminController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpGet("approval/pending")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var residences = await _context.Residences
                .Where(r => !r.IsApprovedBySociety)
                .Include(r => r.User)
                .ToListAsync();
            if (!residences.Any())
            {
                return Ok(new List<PendingApprovalDto>());  // Return empty array
            }
            var dtos = residences.Select(res => new PendingApprovalDto
            {
                ResidentId = res.Id.ToString(),
                Firstname = res.User?.Firstname ?? string.Empty,
                Lastname = res.User?.Lastname ?? string.Empty,
                Cnic = res.User?.Cnic ?? string.Empty,
                Email = res.User?.Email ?? string.Empty,
                Phone = res.User?.Phone ?? string.Empty,
                Residence = new ResidenceSummaryDto
                {
                    AddressLine1 = res.AddressLine1 ?? string.Empty,
                    Block = res.Block ?? string.Empty,
                    Residence = res.Residence1,
                    ResidenceType = res.ResidenceType
                }
            }).ToList();
            return Ok(dtos);
        }

        [HttpGet("residents/{status}")]
        public async Task<IActionResult> GetResidentsByStatus(string status)
        {
            if (status != "approved" && status != "pending")
            {
                return BadRequest("Invalid status. Must be 'approved' or 'pending'.");
            }
            bool isVerified = status == "approved";
            var residences = await _context.Residences
                .Where(r => r.IsApprovedBySociety == isVerified)
                .Include(r => r.User)
                .ToListAsync();
            var dtos = residences.Select(res => new ResidentByStatusDto
            {
                ResidenceId = res.Id,                              // Actual residence ID
                UserId = res.User.Userid,                          // User's unique ID
                Firstname = res.User.Firstname ?? string.Empty,
                Lastname = res.User.Lastname ?? string.Empty,
                Cnic = res.User.Cnic ?? string.Empty,
                Email = res.User.Email ?? string.Empty,
                Phone = res.User.Phone ?? string.Empty,
                AddressLine1 = res.AddressLine1 ?? string.Empty,
                Block = res.Block ?? string.Empty,
                Residence = res.Residence1,
                ResidenceType = res.ResidenceType,
                FlatNumber = res.FlatNumber,
                IsPrimary = res.IsPrimary
            }).ToList();
            return Ok(dtos);
        }

        [HttpGet("vehicle")]
        public async Task<IActionResult> GetAllVehicles()
        {
            var vehicles = await _context.Vehicles
                .Include(v => v.Resident)
                .ThenInclude(r => r.User)
                .ToListAsync();
            var dtos = vehicles.Select(v => new VehicleDto
            {
                VehicleId = v.VehicleId,
                OwnerName = v.Resident?.User != null
                    ? $"{v.Resident.User.Firstname ?? string.Empty} {v.Resident.User.Lastname ?? string.Empty}".Trim()
                    : "N/A",
                VehicleType = v.VehicleType,
                VehicleLicensePlateNumber = v.VehicleLicensePlateNumber,
                VehicleColor = v.VehicleColor,
                VehicleRFIDTagId = v.VehicleRFIDTagId
            }).ToList();
            return Ok(dtos);
        }

        [HttpPatch("approve/{residenceID}")]
        public async Task<IActionResult> ApproveResidence(Guid residenceID)
        {
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == residenceID);
                
            if (residence == null)
            {
                return NotFound(new { Message = "Residence not found" });
            }
            
            residence.IsApprovedBySociety = true;
            residence.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            return Ok(new 
            { 
                Message = "Residence approved successfully",
                ResidenceId = residence.Id,
                FlatNumber = residence.Residence1
            });
        }

        /// <summary>
        /// Get vehicles for a specific residence (Admin only - no ownership check)
        /// </summary>
        [HttpGet("vehicles/residence/{residenceId}")]
        public async Task<IActionResult> GetVehiclesByResidenceAdmin(Guid residenceId)
        {
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == residenceId);

            if (residence == null)
                return NotFound(new { message = "Residence not found." });

            var vehicles = await _context.Vehicles
                .Where(v => v.Residentid.HasValue && v.Residentid.Value == residenceId)
                .Select(v => new
                {
                    v.VehicleId,
                    v.VehicleName,
                    v.VehicleModel,
                    v.VehicleType,
                    v.VehicleLicensePlateNumber,
                    VehicleRFIDTagId = v.VehicleRFIDTagId,
                    v.VehicleColor,
                    ResidenceId = v.Residentid,
                    IsGuest = false,
                    v.CreatedAt,
                    v.UpdatedAt
                })
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return Ok(vehicles);
        }

        /// <summary>
        /// Get guests for a specific residence (Admin only - no ownership check)
        /// </summary>
        [HttpGet("guests/residence/{residenceId}")]
        public async Task<IActionResult> GetGuestsByResidenceAdmin(Guid residenceId)
        {
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == residenceId);

            if (residence == null)
                return NotFound(new { message = "Residence not found." });

            var guests = await _context.Guests
                .Where(g => g.ResidenceId == residenceId)
                .OrderByDescending(g => g.CreatedAt)
                .Select(g => new
                {
                    g.GuestId,
                    g.GuestName,
                    g.GuestPhoneNumber,
                    g.Gender,
                    g.Eta,
                    g.CheckoutTime,
                    g.ActualArrivalTime,
                    g.Status,
                    g.IsVerified,
                    g.VisitCompleted,
                    g.CreatedAt,
                    IsExpired = g.CheckoutTime < DateTime.UtcNow,
                    IsActive = g.IsVerified && !g.VisitCompleted && g.CheckoutTime >= DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(guests);
        }
    }
}
