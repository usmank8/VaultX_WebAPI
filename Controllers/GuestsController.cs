using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GuestController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public GuestController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpGet("guest/mine")]
        [Authorize(Roles = "resident")]
        public async Task<IActionResult> GetAllGuests()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var residences = await _context.Residences
                .Where(r => r.User.Userid == userId)
                .ToListAsync();
            var residenceIds = residences.Select(r => r.Id).ToList();
            if (!residenceIds.Any()) return Ok(new List<GuestWithVehicleDto>());
            var guests = await _context.Guests
                .Where(g => residenceIds.Contains(g.Residence.Id))
                .Include(g => g.Vehicle)
                .Include(g => g.Residence)
                .ToListAsync();
            var dtos = guests.Select(g => new GuestWithVehicleDto
            {
                GuestId = g.GuestId,
                GuestName = g.GuestName,
                Eta = g.Eta,
                VehicleId = g.Vehicle?.VehicleId,
                VehicleModel = g.Vehicle?.VehicleModel,
                VehicleLicensePlateNumber = g.Vehicle?.VehicleLicensePlateNumber,
                VehicleColor = g.Vehicle?.VehicleColor,
                IsGuest = g.Vehicle?.IsGuest
            }).ToList();
            return Ok(dtos);
        }

        //[HttpPost("register")]
        //[Authorize(Roles = "resident")]
        //public async Task<IActionResult> RegisterGuest([FromBody] AddGuestDto dto)
        //{
        //    var userId = User.FindFirst("userid")?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized();
        //    }
        //    if (!DateTime.TryParse(dto.Eta, out var eta))
        //    {
        //        return BadRequest("Invalid ETA format.");
        //    }
        //    var residence = await _context.Residences
        //        .Include(r => r.User)
        //        .FirstOrDefaultAsync(r => r.User.Userid == userId && r.IsPrimary);
        //    if (residence == null)
        //    {
        //        return NotFound("Primary residence not found for the user.");
        //    }
        //    Vehicle? vehicle = null;
        //    if (dto.Vehicle != null)
        //    {
        //        vehicle = new Vehicle
        //        {
        //            VehicleName = dto.Vehicle.VehicleName,
        //            VehicleModel = dto.Vehicle.VehicleModel,
        //            VehicleType = dto.Vehicle.VehicleType,
        //            VehicleLicensePlateNumber = dto.Vehicle.VehicleLicensePlateNumber,
        //            VehicleRFIDTagId = dto.Vehicle.VehicleRFIDTagId,
        //            VehicleColor = dto.Vehicle.VehicleColor,
        //            IsGuest = true
        //        };
        //        _context.Vehicles.Add(vehicle);
        //        await _context.SaveChangesAsync();
        //    }
        //    var guest = new Guest
        //    {
        //        GuestName = dto.GuestName,
        //        GuestPhoneNumber = dto.GuestPhoneNumber,
        //        Eta = eta,
        //        VisitCompleted = dto.VisitCompleted,
        //        Residence = residence,
        //        Vehicle = vehicle,
        //        QrCode = "processing"
        //    };
        //    try
        //    {
        //        _context.Guests.Add(guest);
        //        await _context.SaveChangesAsync();
        //        var qrPayload = JsonSerializer.Serialize(new { GuestId = guest.GuestId, Eta = guest.Eta });
        //        var qrGenerator = new QRCodeGenerator();
        //        var qrCodeData = qrGenerator.CreateQrCode(qrPayload, QRCodeGenerator.ECCLevel.Q);
        //        //var qrCode = new QRCode(qrCodeData);
        //        //using var qrCodeImage = qrCode.GetGraphic(20);
        //        using var stream = new MemoryStream();
        //        qrCodeImage.Save(stream, ImageFormat.Png);
        //        var qrCodeBytes = stream.ToArray();
        //        var qrCodeImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        //        return Ok(new QrCodeResponse { QrCodeImage = qrCodeImageBase64 });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, "Failed to register guest or generate QR code.");
        //    }
        //}

        [HttpPost("verify")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> VerifyGuest([FromBody] VerifyGuestDto dto)
        {
            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.GuestId == dto.GuestId);
            if (guest == null)
            {
                return Ok(new VerifyResponse { Valid = false, Reason = "Guest not found." });
            }
            if (guest.VisitCompleted)
            {
                return Ok(new VerifyResponse { Valid = false, Reason = "Visit already completed." });
            }
            var now = DateTime.UtcNow;
            var timeDiff = Math.Abs((now - guest.Eta).TotalMilliseconds);
            var oneHour = 1000 * 60 * 60;
            if (timeDiff > oneHour)
            {
                return Ok(new VerifyResponse { Valid = false, Reason = "Guest not within allowed ETA window." });
            }
            guest.IsVerified = true;
            await _context.SaveChangesAsync();
            return Ok(new VerifyResponse { Valid = true });
        }

        [HttpGet("guest/{id}")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetGuestById(string id)
        {
            var guest = await _context.Guests
                .Include(g => g.Residence)
                .Include(g => g.Vehicle)
                .FirstOrDefaultAsync(g => g.GuestId == id);
            if (guest == null)
            {
                return NotFound();
            }
            var dto = new GuestDetailDto
            {
                GuestId = guest.GuestId,
                GuestName = guest.GuestName,
                GuestPhoneNumber = guest.GuestPhoneNumber,
                Eta = guest.Eta,
                VisitCompleted = guest.VisitCompleted,
                Residence = new GuestResidenceDto
                {
                    Id = guest.Residence.Id.ToString(),
                    AddressLine1 = guest.Residence.AddressLine1,
                    AddressLine2 = guest.Residence.AddressLine2,
                    //City = guest.Residence.City,
                    //State = guest.Residence.State,
                    //Country = guest.Residence.Country,
                    //PostalCode = guest.Residence.PostalCode,
                    FlatNumber = guest.Residence.FlatNumber,
                    Block = guest.Residence.Block
                },
                GuestVehicle = guest.Vehicle != null ? new GuestVehicleDto
                {
                    VehicleId = guest.Vehicle.VehicleId,
                    VehicleType = guest.Vehicle.VehicleType,
                    VehicleModel = guest.Vehicle.VehicleModel,
                    VehicleName = guest.Vehicle.VehicleName,
                    VehicleLicensePlateNumber = guest.Vehicle.VehicleLicensePlateNumber,
                    VehicleRFIDTagId = guest.Vehicle.VehicleRFIDTagId,
                    VehicleColor = guest.Vehicle.VehicleColor,
                    IsGuest = guest.Vehicle.IsGuest
                } : null
            };
            return Ok(dto);
        }

        [HttpGet("all")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetGuests([FromQuery] int skip = 0, [FromQuery] int? offset = null, [FromQuery] int limit = 10)
        {
            var effectiveSkip = offset ?? skip;
            var guests = await _context.Guests
                .Include(g => g.Vehicle)
                .Include(g => g.Residence)
                .OrderByDescending(g => g.Eta)
                .Skip(effectiveSkip)
                .Take(limit)
                .ToListAsync();
            var total = await _context.Guests.CountAsync();
            var data = guests.Select(guest => new GuestDetailDto
            {
                GuestId = guest.GuestId,
                GuestName = guest.GuestName,
                GuestPhoneNumber = guest.GuestPhoneNumber,
                Eta = guest.Eta,
                VisitCompleted = guest.VisitCompleted,
                Residence = new GuestResidenceDto
                {
                    Id = guest.Residence.Id.ToString(),
                    AddressLine1 = guest.Residence.AddressLine1,
                    AddressLine2 = guest.Residence.AddressLine2,
                    FlatNumber = guest.Residence.FlatNumber,
                    Block = guest.Residence.Block
                },
                GuestVehicle = guest.Vehicle != null ? new GuestVehicleDto
                {
                    VehicleId = guest.Vehicle.VehicleId,
                    VehicleType = guest.Vehicle.VehicleType,
                    VehicleModel = guest.Vehicle.VehicleModel,
                    VehicleName = guest.Vehicle.VehicleName,
                    VehicleLicensePlateNumber = guest.Vehicle.VehicleLicensePlateNumber,
                    VehicleRFIDTagId = guest.Vehicle.VehicleRFIDTagId,
                    VehicleColor = guest.Vehicle.VehicleColor,
                    IsGuest = guest.Vehicle.IsGuest
                } : null
            }).ToList();
            var page = (effectiveSkip / limit) + 1;
            return Ok(new PaginatedGuestsDto
            {
                Data = data,
                Total = total,
                Skip = effectiveSkip,
                Limit = limit,
                Page = page
            });
        }

        [HttpGet("verified")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetVerifiedGuests([FromQuery] int skip = 0, [FromQuery] int? offset = null, [FromQuery] int limit = 10)
        {
            var effectiveSkip = offset ?? skip;
            var guests = await _context.Guests
                .Where(g => g.IsVerified)
                .Include(g => g.Vehicle)
                .Include(g => g.Residence)
                .OrderByDescending(g => g.Eta)
                .Skip(effectiveSkip)
                .Take(limit)
                .ToListAsync();
            var total = await _context.Guests.CountAsync(g => g.IsVerified);
            var data = guests.Select(guest => new GuestDetailDto
            {
                GuestId = guest.GuestId,
                GuestName = guest.GuestName,
                GuestPhoneNumber = guest.GuestPhoneNumber,
                Eta = guest.Eta,
                VisitCompleted = guest.VisitCompleted,
                Residence = new GuestResidenceDto
                {
                    Id = guest.Residence.Id.ToString(),
                    AddressLine1 = guest.Residence.AddressLine1,
                    AddressLine2 = guest.Residence.AddressLine2,
                    FlatNumber = guest.Residence.FlatNumber,
                    Block = guest.Residence.Block
                },
                GuestVehicle = guest.Vehicle != null ? new GuestVehicleDto
                {
                    VehicleId = guest.Vehicle.VehicleId,
                    VehicleType = guest.Vehicle.VehicleType,
                    VehicleModel = guest.Vehicle.VehicleModel,
                    VehicleName = guest.Vehicle.VehicleName,
                    VehicleLicensePlateNumber = guest.Vehicle.VehicleLicensePlateNumber,
                    VehicleRFIDTagId = guest.Vehicle.VehicleRFIDTagId,
                    VehicleColor = guest.Vehicle.VehicleColor,
                    IsGuest = guest.Vehicle.IsGuest
                } : null
            }).ToList();
            var page = (effectiveSkip / limit) + 1;
            return Ok(new PaginatedGuestsDto
            {
                Data = data,
                Total = total,
                Skip = effectiveSkip,
                Limit = limit,
                Page = page
            });
        }
    }
}

