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
    public class VehicleController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public VehicleController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpPost("add")]
        [Authorize(Roles = "resident")]
        public async Task<IActionResult> AddVehicle([FromBody] AddVehicleDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == dto.ResidenceId && r.Userid == userId);

            if (residence == null)
            {
                return NotFound("Residence not found or does not belong to you.");
            }

            var vehicle = new Vehicle
            {
                VehicleId = Guid.NewGuid().ToString(),
                VehicleName = dto.VehicleName,
                VehicleModel = dto.VehicleModel,
                VehicleType = dto.VehicleType,
                VehicleLicensePlateNumber = dto.VehicleLicensePlateNumber,
                VehicleRFIDTagId = dto.VehicleRFIDTagId,
                VehicleColor = dto.VehicleColor,
                Residentid = dto.ResidenceId,
                IsGuest = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Vehicle added successfully.", VehicleId = vehicle.VehicleId });
        }

        [HttpGet]
        [Authorize(Roles = "resident")]
        public async Task<IActionResult> GetAllVehiclesByUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");
            
            var ownerName = $"{user.Firstname} {user.Lastname}";

            var residences = await _context.Residences
                .Where(r => r.Userid == userId)
                .ToListAsync();

            var residenceIds = residences.Select(r => r.Id).ToList();

            if (!residenceIds.Any()) return Ok(new List<VehicleDto>());

            // ✅ ADD .Include() TO LOAD RESIDENCE DATA
            var vehicles = await _context.Vehicles
                .Include(v => v.Resident)  // Load Residence navigation property
                .Where(v => residenceIds.Contains(v.Residentid))
                .ToListAsync();

            var dtos = vehicles.Select(v => new VehicleDto
            {
                VehicleId = v.VehicleId,
                VehicleName = v.VehicleName,
                VehicleModel = v.VehicleModel,
                VehicleType = v.VehicleType,
                VehicleLicensePlateNumber = v.VehicleLicensePlateNumber,
                VehicleRFIDTagId = v.VehicleRFIDTagId,
                VehicleColor = v.VehicleColor,
                IsGuest = v.IsGuest,
                OwnerName = ownerName,
                
                // ✅ ADD ONLY THESE 3 PROPERTIES:
                ResidenceId = v.Residentid,
                ResidenceName = v.Resident?.AddressLine1 ?? "Unknown",
                IsPrimaryResidence = v.Resident?.IsPrimary ?? false
            }).ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Get all vehicles for a specific residence (Admin/Employee only)
        /// Filters out guest vehicles
        /// </summary>
        [HttpGet("residence/{residenceId}")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetVehiclesByResidence(Guid residenceId)
        {
            // Verify residence exists
            var residenceExists = await _context.Residences.AnyAsync(r => r.Id == residenceId);
            if (!residenceExists)
            {
                return NotFound(new { message = "Residence not found" });
            }

            var vehicles = await _context.Vehicles
                .Where(v => v.Residentid == residenceId && v.IsGuest == false)  // ✅ Fixed: Residentid (not ResidenceId)
                .Select(v => new
                {
                    vehicleId = v.VehicleId,
                    vehicleName = v.VehicleName,
                    vehicleType = v.VehicleType,
                    vehicleModel = v.VehicleModel,
                    vehicleLicensePlateNumber = v.VehicleLicensePlateNumber,
                    vehicleRFIDTagId = v.VehicleRFIDTagId,
                    vehicleColor = v.VehicleColor,
                    isGuest = v.IsGuest
                })
                .ToListAsync();

            return Ok(vehicles);
        }
    }

    public class VehicleDto
    {
        public string VehicleId { get; set; }
        public string OwnerName { get; set; }
        public string VehicleName { get; set; }
        public string VehicleModel { get; set; }
        public string VehicleType { get; set; }
        public string VehicleLicensePlateNumber { get; set; }
        public string VehicleRFIDTagId { get; set; }
        public string VehicleColor { get; set; }
        public bool IsGuest { get; set; }
        
        // ✅ ADD ONLY THESE 3 PROPERTIES:
        public Guid ResidenceId { get; set; }
        public string ResidenceName { get; set; } = string.Empty;
        public bool IsPrimaryResidence { get; set; }
    }
}
