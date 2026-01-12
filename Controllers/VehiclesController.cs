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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehiclesController : ControllerBase
    {
        private readonly VaultxDbContext _context;
        private const int MAX_VEHICLES_PER_RESIDENCE = 4;

        public VehiclesController(VaultxDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all vehicles for the current user (across all their residences)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserVehicles()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            // Get all user's residence IDs first
            var userResidenceIds = await _context.Residences
                .Where(r => r.Userid == userId)
                .Select(r => r.Id)
                .ToListAsync();

            if (!userResidenceIds.Any())
                return Ok(new List<object>()); // Return empty list if no residences

            // Get vehicles for all user's residences
            var vehicles = await _context.Vehicles
                .Include(v => v.Resident)
                .Where(v => v.Residentid.HasValue && userResidenceIds.Contains(v.Residentid.Value))
                .Select(v => new
                {
                    v.VehicleId,
                    v.VehicleName,
                    v.VehicleModel,
                    v.VehicleType,
                    v.VehicleLicensePlateNumber,
                    VehicleRfidtagId = v.VehicleRFIDTagId,
                    v.VehicleColor,
                    ResidenceId = v.Residentid,
                    ResidenceName = v.Resident != null ? v.Resident.Residence1 : null,
                    ResidenceBlock = v.Resident != null ? v.Resident.Block : null,
                    v.CreatedAt,
                    v.UpdatedAt
                })
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return Ok(vehicles);
        }

        /// <summary>
        /// Get vehicles for a specific residence
        /// </summary>
        [HttpGet("residence/{residenceId}")]
        public async Task<IActionResult> GetVehiclesByResidence(Guid residenceId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            // Verify residence belongs to user
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == residenceId && r.Userid == userId);

            if (residence == null)
                return NotFound(new { message = "Residence not found or does not belong to you." });

            var vehicles = await _context.Vehicles
                .Where(v => v.Residentid.HasValue && v.Residentid.Value == residenceId)
                .Select(v => new
                {
                    v.VehicleId,
                    v.VehicleName,
                    v.VehicleModel,
                    v.VehicleType,
                    v.VehicleLicensePlateNumber,
                    VehicleRfidtagId = v.VehicleRFIDTagId,
                    v.VehicleColor,
                    ResidenceId = v.Residentid,
                    v.CreatedAt,
                    v.UpdatedAt
                })
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return Ok(vehicles);
        }

        /// <summary>
        /// Get a specific vehicle by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicle(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            var vehicle = await _context.Vehicles
                .Include(v => v.Resident)
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound(new { message = "Vehicle not found" });

            // Verify vehicle belongs to user's residence
            if (vehicle.Resident == null || vehicle.Resident.Userid != userId)
                return Forbid();

            return Ok(new
            {
                vehicle.VehicleId,
                vehicle.VehicleName,
                vehicle.VehicleModel,
                vehicle.VehicleType,
                vehicle.VehicleLicensePlateNumber,
                VehicleRfidtagId = vehicle.VehicleRFIDTagId,
                vehicle.VehicleColor,
                ResidenceId = vehicle.Residentid,
                vehicle.CreatedAt,
                vehicle.UpdatedAt
            });
        }

        /// <summary>
        /// Add a new vehicle to a residence (max 4 per residence)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddVehicle([FromBody] AddVehicleDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Determine which residence to add vehicle to
            Residence? residence;

            if (dto.ResidenceId.HasValue)
            {
                // Use specified residence
                residence = await _context.Residences
                    .FirstOrDefaultAsync(r => r.Id == dto.ResidenceId.Value && r.Userid == userId);

                if (residence == null)
                    return NotFound(new { message = "Residence not found or does not belong to you." });
            }
            else
            {
                // Use primary residence as default
                residence = await _context.Residences
                    .FirstOrDefaultAsync(r => r.Userid == userId && r.IsPrimary);

                if (residence == null)
                    return NotFound(new { message = "No primary residence found. Please add a residence first." });
            }

            // Check if residence is approved
            if (!residence.IsApprovedBySociety)
                return BadRequest(new { message = "Cannot add vehicles to unapproved residence." });

            // CHECK: Maximum 4 vehicles per residence
            var vehicleCount = await _context.Vehicles
                .CountAsync(v => v.Residentid.HasValue && v.Residentid.Value == residence.Id);

            if (vehicleCount >= MAX_VEHICLES_PER_RESIDENCE)
            {
                return BadRequest(new 
                { 
                    message = $"Maximum of {MAX_VEHICLES_PER_RESIDENCE} vehicles allowed per residence. You already have {vehicleCount} vehicles.",
                    currentCount = vehicleCount,
                    maxAllowed = MAX_VEHICLES_PER_RESIDENCE
                });
            }

            // Check for duplicate license plate
            var existingVehicle = await _context.Vehicles
                .FirstOrDefaultAsync(v => v.VehicleLicensePlateNumber == dto.VehicleLicensePlateNumber);

            if (existingVehicle != null)
                return BadRequest(new { message = "A vehicle with this license plate already exists." });

            // Check for duplicate RFID tag if provided
            if (!string.IsNullOrEmpty(dto.VehicleRfidTagId))
            {
                var existingRfid = await _context.Vehicles
                    .FirstOrDefaultAsync(v => v.VehicleRFIDTagId == dto.VehicleRfidTagId);

                if (existingRfid != null)
                    return BadRequest(new { message = "A vehicle with this RFID tag already exists." });
            }

            var vehicle = new Vehicle
            {
                VehicleId = Guid.NewGuid().ToString(),
                VehicleName = dto.VehicleName ?? string.Empty,
                VehicleModel = dto.VehicleModel ?? string.Empty,
                VehicleType = dto.VehicleType ?? string.Empty,
                VehicleLicensePlateNumber = dto.VehicleLicensePlateNumber ?? string.Empty,
                VehicleRFIDTagId = dto.VehicleRfidTagId ?? string.Empty,
                VehicleColor = dto.VehicleColor ?? string.Empty,
                Residentid = residence.Id,
                IsGuest = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.VehicleId }, new
            {
                message = "Vehicle added successfully",
                vehicle = new
                {
                    vehicle.VehicleId,
                    vehicle.VehicleName,
                    vehicle.VehicleModel,
                    vehicle.VehicleType,
                    vehicle.VehicleLicensePlateNumber,
                    VehicleRfidtagId = vehicle.VehicleRFIDTagId,
                    vehicle.VehicleColor,
                    ResidenceId = vehicle.Residentid,
                    vehicle.CreatedAt
                },
                vehicleCount = vehicleCount + 1,
                remainingSlots = MAX_VEHICLES_PER_RESIDENCE - vehicleCount - 1
            });
        }

        /// <summary>
        /// Update a vehicle
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicle(string id, [FromBody] UpdateVehicleDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            var vehicle = await _context.Vehicles
                .Include(v => v.Resident)
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound(new { message = "Vehicle not found" });

            // Verify vehicle belongs to user's residence
            if (vehicle.Resident == null || vehicle.Resident.Userid != userId)
                return Forbid();

            // Update fields if provided
            if (!string.IsNullOrEmpty(dto.VehicleName))
                vehicle.VehicleName = dto.VehicleName;
            if (!string.IsNullOrEmpty(dto.VehicleModel))
                vehicle.VehicleModel = dto.VehicleModel;
            if (!string.IsNullOrEmpty(dto.VehicleType))
                vehicle.VehicleType = dto.VehicleType;
            if (!string.IsNullOrEmpty(dto.VehicleLicensePlateNumber))
                vehicle.VehicleLicensePlateNumber = dto.VehicleLicensePlateNumber;
            if (!string.IsNullOrEmpty(dto.VehicleRfidTagId))
                vehicle.VehicleRFIDTagId = dto.VehicleRfidTagId;
            if (!string.IsNullOrEmpty(dto.VehicleColor))
                vehicle.VehicleColor = dto.VehicleColor;

            vehicle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Vehicle updated successfully",
                vehicle = new
                {
                    vehicle.VehicleId,
                    vehicle.VehicleName,
                    vehicle.VehicleModel,
                    vehicle.VehicleType,
                    vehicle.VehicleLicensePlateNumber,
                    VehicleRfidtagId = vehicle.VehicleRFIDTagId,
                    vehicle.VehicleColor,
                    ResidenceId = vehicle.Residentid,
                    vehicle.UpdatedAt
                }
            });
        }

        /// <summary>
        /// Delete a vehicle
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(string id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            var vehicle = await _context.Vehicles
                .Include(v => v.Resident)
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
                return NotFound(new { message = "Vehicle not found" });

            // Verify vehicle belongs to user's residence
            if (vehicle.Resident == null || vehicle.Resident.Userid != userId)
                return Forbid();

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Vehicle deleted successfully" });
        }

        /// <summary>
        /// Get vehicle count for a residence
        /// </summary>
        [HttpGet("residence/{residenceId}/count")]
        public async Task<IActionResult> GetVehicleCount(Guid residenceId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            // Verify residence belongs to user
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == residenceId && r.Userid == userId);

            if (residence == null)
                return NotFound(new { message = "Residence not found" });

            var count = await _context.Vehicles.CountAsync(v => v.Residentid.HasValue && v.Residentid.Value == residenceId);

            return Ok(new
            {
                residenceId,
                vehicleCount = count,
                maxAllowed = MAX_VEHICLES_PER_RESIDENCE,
                remainingSlots = MAX_VEHICLES_PER_RESIDENCE - count,
                canAddMore = count < MAX_VEHICLES_PER_RESIDENCE
            });
        }
    }
}
