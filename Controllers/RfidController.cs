using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RfidController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public RfidController(VaultxDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get vehicle, residence, and owner details by RFID tag (excludes guest vehicles)
        /// </summary>
        [HttpGet("{rfidTag}")]
        public async Task<IActionResult> GetByRfid(string rfidTag)
        {
            if (string.IsNullOrWhiteSpace(rfidTag))
            {
                return BadRequest(new { message = "RFID tag is required" });
            }

            var vehicle = await _context.Vehicles
                .Include(v => v.Resident)
                    .ThenInclude(r => r.User)
                .Where(v => v.VehicleRFIDTagId == rfidTag && v.IsGuest == false)
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehicle not found or is a guest vehicle" });
            }

            var result = new RfidLookupResponse
            {
                Vehicle = new VehicleInfo
                {
                    VehicleId = vehicle.VehicleId,
                    VehicleName = vehicle.VehicleName,
                    VehicleType = vehicle.VehicleType,
                    VehicleModel = vehicle.VehicleModel,
                    VehicleColor = vehicle.VehicleColor,
                    VehicleLicensePlateNumber = vehicle.VehicleLicensePlateNumber,
                    VehicleRFIDTagId = vehicle.VehicleRFIDTagId
                },
                Residence = vehicle.Resident != null ? new ResidenceInfo
                {
                    ResidenceId = vehicle.Resident.Id,
                    ResidenceType = vehicle.Resident.ResidenceType,
                    ResidenceName = vehicle.Resident.Residence1,
                    AddressLine1 = vehicle.Resident.AddressLine1,
                    AddressLine2 = vehicle.Resident.AddressLine2,
                    FlatNumber = vehicle.Resident.FlatNumber,
                    Block = vehicle.Resident.Block,
                    IsPrimary = vehicle.Resident.IsPrimary,
                    IsApproved = vehicle.Resident.IsApprovedBySociety
                } : null,
                Owner = vehicle.Resident?.User != null ? new OwnerInfo
                {
                    UserId = vehicle.Resident.User.Userid,
                    FullName = $"{vehicle.Resident.User.Firstname} {vehicle.Resident.User.Lastname}".Trim(),
                    Email = vehicle.Resident.User.Email,
                    Phone = vehicle.Resident.User.Phone,
                    Cnic = vehicle.Resident.User.Cnic
                } : null
            };

            return Ok(result);
        }

        /// <summary>
        /// Record vehicle access (entry/exit randomly assigned) by RFID tag
        /// </summary>
        [HttpPost("access/{rfidTag}")]
        public async Task<IActionResult> RecordAccess(string rfidTag, [FromQuery] string? gateName = null)
        {
            if (string.IsNullOrWhiteSpace(rfidTag))
            {
                return BadRequest(new { message = "RFID tag is required" });
            }

            var vehicle = await _context.Vehicles
                .Where(v => v.VehicleRFIDTagId == rfidTag && v.IsGuest == false)
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehicle not found or is a guest vehicle" });
            }

            var accessType = Random.Shared.Next(2) == 0 ? "Entry" : "Exit";

            var log = new VehicleAccessLog
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicle.VehicleId,
                AccessType = accessType,
                Timestamp = DateTime.UtcNow,
                GateName = gateName,
                RecordedBy = null
            };

            _context.VehicleAccessLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Vehicle {accessType.ToLower()} recorded successfully",
                logId = log.Id,
                vehicleId = vehicle.VehicleId,
                licensePlate = vehicle.VehicleLicensePlateNumber,
                accessType = accessType,
                timestamp = log.Timestamp,
                gate = gateName
            });
        }

        /// <summary>
        /// Get vehicle access history for the last 1 hour
        /// </summary>
        [HttpGet("history/hour")]
        // [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetHistoryLastHour()
        {
            return await GetAccessHistory(DateTime.UtcNow.AddHours(-1));
        }

        /// <summary>
        /// Get vehicle access history for the last 1 day
        /// </summary>
        [HttpGet("history/day")]
        // [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetHistoryLastDay()
        {
            return await GetAccessHistory(DateTime.UtcNow.AddDays(-1));
        }

        /// <summary>
        /// Get vehicle access history for the last 1 week
        /// </summary>
        [HttpGet("history/week")]
        // [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetHistoryLastWeek()
        {
            return await GetAccessHistory(DateTime.UtcNow.AddDays(-7));
        }

        /// <summary>
        /// Get vehicle access history for the last 1 month
        /// </summary>
        [HttpGet("history/month")]
        // [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetHistoryLastMonth()
        {
            return await GetAccessHistory(DateTime.UtcNow.AddMonths(-1));
        }

        private async Task<IActionResult> GetAccessHistory(DateTime fromDate)
        {
            var logs = await _context.VehicleAccessLogs
                .Include(l => l.Vehicle)
                    .ThenInclude(v => v.Resident)
                        .ThenInclude(r => r.User)
                .Where(l => l.Timestamp >= fromDate)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new AccessLogResponse
                {
                    LogId = l.Id,
                    AccessType = l.AccessType,
                    Timestamp = l.Timestamp,
                    GateName = l.GateName,
                    Vehicle = new VehicleInfo
                    {
                        VehicleId = l.Vehicle.VehicleId,
                        VehicleName = l.Vehicle.VehicleName,
                        VehicleType = l.Vehicle.VehicleType,
                        VehicleModel = l.Vehicle.VehicleModel,
                        VehicleColor = l.Vehicle.VehicleColor,
                        VehicleLicensePlateNumber = l.Vehicle.VehicleLicensePlateNumber,
                        VehicleRFIDTagId = l.Vehicle.VehicleRFIDTagId
                    },
                    ResidenceInfo = l.Vehicle.Resident != null ? $"{l.Vehicle.Resident.Block} - {l.Vehicle.Resident.FlatNumber}" : null,
                    OwnerName = l.Vehicle.Resident != null && l.Vehicle.Resident.User != null
                        ? $"{l.Vehicle.Resident.User.Firstname} {l.Vehicle.Resident.User.Lastname}".Trim()
                        : null
                })
                .ToListAsync();

            return Ok(new
            {
                count = logs.Count,
                fromDate = fromDate,
                toDate = DateTime.UtcNow,
                logs = logs
            });
        }

        /// <summary>
        /// Get access history for a specific vehicle by RFID
        /// </summary>
        [HttpGet("history/vehicle/{rfidTag}")]
        // [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetVehicleHistory(string rfidTag, [FromQuery] string period = "day")
        {
            var vehicle = await _context.Vehicles
                .Where(v => v.VehicleRFIDTagId == rfidTag && v.IsGuest == false)
                .FirstOrDefaultAsync();

            if (vehicle == null)
            {
                return NotFound(new { message = "Vehicle not found or is a guest vehicle" });
            }

            var fromDate = period.ToLower() switch
            {
                "hour" => DateTime.UtcNow.AddHours(-1),
                "day" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddMonths(-1),
                _ => DateTime.UtcNow.AddDays(-1)
            };

            var logs = await _context.VehicleAccessLogs
                .Where(l => l.VehicleId == vehicle.VehicleId && l.Timestamp >= fromDate)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new
                {
                    logId = l.Id,
                    accessType = l.AccessType,
                    timestamp = l.Timestamp,
                    gateName = l.GateName
                })
                .ToListAsync();

            return Ok(new
            {
                vehicle = new VehicleInfo
                {
                    VehicleId = vehicle.VehicleId,
                    VehicleName = vehicle.VehicleName,
                    VehicleType = vehicle.VehicleType,
                    VehicleModel = vehicle.VehicleModel,
                    VehicleColor = vehicle.VehicleColor,
                    VehicleLicensePlateNumber = vehicle.VehicleLicensePlateNumber,
                    VehicleRFIDTagId = vehicle.VehicleRFIDTagId
                },
                period = period,
                count = logs.Count,
                logs = logs
            });
        }
    }

    public class AccessLogResponse
    {
        public Guid LogId { get; set; }
        public string AccessType { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public string? GateName { get; set; }
        public VehicleInfo Vehicle { get; set; } = null!;
        public string? ResidenceInfo { get; set; }
        public string? OwnerName { get; set; }
    }

    public class RfidLookupResponse
    {
        public VehicleInfo Vehicle { get; set; } = null!;
        public ResidenceInfo? Residence { get; set; }
        public OwnerInfo? Owner { get; set; }
    }

    public class VehicleInfo
    {
        public string VehicleId { get; set; } = null!;
        public string VehicleName { get; set; } = null!;
        public string VehicleType { get; set; } = null!;
        public string VehicleModel { get; set; } = null!;
        public string VehicleColor { get; set; } = null!;
        public string VehicleLicensePlateNumber { get; set; } = null!;
        public string VehicleRFIDTagId { get; set; } = null!;
    }

    public class ResidenceInfo
    {
        public Guid ResidenceId { get; set; }
        public string ResidenceType { get; set; } = null!;
        public string ResidenceName { get; set; } = null!;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? FlatNumber { get; set; }
        public string? Block { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsApproved { get; set; }
    }

    public class OwnerInfo
    {
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Cnic { get; set; }
    }
}
