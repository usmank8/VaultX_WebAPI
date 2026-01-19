using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Claims;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuestsController : ControllerBase
    {
        private readonly VaultxDbContext _context;
        private readonly ILogger<GuestsController> _logger;

        public GuestsController(VaultxDbContext context, ILogger<GuestsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================
        // GET: api/Guests (All guests - Admin only)
        // ============================================
        [HttpGet]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetAllGuests()
        {
            try
            {
                // Auto-complete expired guests before fetching
                await AutoCompleteExpiredGuests();

                var guests = await _context.Guests
                    .Include(g => g.User)
                    .Include(g => g.Residence)
                    .Include(g => g.Vehicle)
                    .OrderByDescending(g => g.CreatedAt)
                    .Select(g => new
                    {
                        g.GuestId,
                        g.GuestName,
                        g.GuestPhoneNumber,
                        Gender = g.Gender ?? "",
                        g.Eta,
                        g.CheckoutTime,
                        g.ActualArrivalTime,
                        Status = g.Status ?? "pending",
                        QrCode = Convert.ToBase64String(g.QrCode),
                        Userid = g.Userid ?? "",
                        g.ResidenceId,
                        g.VehicleId,
                        g.IsVerified,
                        g.VisitCompleted,
                        g.CreatedAt,
                        g.UpdatedAt,
                        User = g.User != null ? new
                        {
                            g.User.Userid,
                            g.User.Firstname,
                            g.User.Lastname,
                            g.User.Email
                        } : null,
                        Residence = g.Residence != null ? new
                        {
                            g.Residence.Id,
                            g.Residence.FlatNumber,
                            g.Residence.Block
                        } : null
                    })
                    .ToListAsync();

                return Ok(guests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all guests");
                return StatusCode(500, new { message = "Error retrieving guests", error = ex.Message });
            }
        }

        // ============================================
        // GET: api/Guests/my-guests (Resident's guests only)
        // ============================================
        [HttpGet("my-guests")]
        [Authorize(Roles = "resident")]
        public async Task<IActionResult> GetMyGuests()
        {
            try
            {
                var residentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(residentUserId))
                    return Unauthorized(new { message = "User not authenticated" });

                // Auto-complete expired guests before fetching
                await AutoCompleteExpiredGuests();

                var guests = await _context.Guests
                    .Where(g => g.Userid == residentUserId)
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
                        QrCode = Convert.ToBase64String(g.QrCode),
                        g.IsVerified,
                        g.VisitCompleted,
                        g.CreatedAt,
                        IsExpired = DateTime.UtcNow > g.CheckoutTime,
                        IsLate = g.ActualArrivalTime != null && g.ActualArrivalTime > g.Eta,
                        TimeRemaining = g.CheckoutTime > DateTime.UtcNow 
                            ? (g.CheckoutTime - DateTime.UtcNow).TotalHours 
                            : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalGuests = guests.Count,
                    pendingGuests = guests.Count(g => g.Status == "pending"),
                    activeGuests = guests.Count(g => g.Status == "active"),
                    completedGuests = guests.Count(g => g.Status == "completed"),
                    guests = guests
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving my guests");
                return StatusCode(500, new { message = "Error retrieving guests", error = ex.Message });
            }
        }

        // ============================================
        // POST: api/Guests/register (Register new guest)
        // ============================================
        [HttpPost("register")]
        [Authorize(Roles = "resident")]
        public async Task<IActionResult> RegisterGuest([FromBody] AddGuestDto dto)
        {
            try
            {
                var residentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(residentUserId))
                    return Unauthorized(new { message = "User not authenticated" });

                // Verify residence belongs to this resident
                var residence = await _context.Residences
                    .FirstOrDefaultAsync(r => r.Id == dto.ResidenceId && r.Userid == residentUserId);

                if (residence == null)
                    return BadRequest(new { message = "Invalid residence or you don't have permission" });

                // ✅ VALIDATION: CheckoutTime must be after ETA
                if (dto?.CheckoutTime <= dto.Eta)
                {
                    return BadRequest(new
                    {
                        message = "Checkout time must be after expected arrival time",
                        eta = dto.Eta,
                        checkoutTime = dto.CheckoutTime
                    });
                }

                // ✅ VALIDATION: CheckoutTime can't be in the past
                if (dto?.CheckoutTime < DateTime.UtcNow)
                {
                    return BadRequest(new
                    {
                        message = "Checkout time cannot be in the past",
                        checkoutTime = dto.CheckoutTime,
                        currentTime = DateTime.UtcNow
                    });
                }

                // ✅ AUTO-CREATE VEHICLE if vehicle details are provided
                string? vehicleId = null;  // Removed dto.VehicleId

                if (!string.IsNullOrEmpty(dto.VehicleName) ||
                    !string.IsNullOrEmpty(dto.VehicleModel) ||
                    !string.IsNullOrEmpty(dto.VehicleLicensePlateNumber))
                {
                    var vehicle = new Vehicle
                    {
                        VehicleId = Guid.NewGuid().ToString(),
                        VehicleName = dto.VehicleName ?? "Guest Vehicle",
                        VehicleModel = dto.VehicleModel ?? "N/A",
                        VehicleLicensePlateNumber = dto.VehicleLicensePlateNumber ?? "N/A",
                        VehicleType = dto.VehicleType ?? "Sedan",
                        VehicleColor = dto.VehicleColor ?? "N/A",
                        VehicleRFIDTagId = "",  // Guests don't have RFID tags
                        IsGuest = true,  // ✅ Mark as guest vehicle
                        Residentid = residence.Id,  // Link to residence
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Vehicles.AddAsync(vehicle);
                    await _context.SaveChangesAsync();

                    vehicleId = vehicle.VehicleId;

                    _logger.LogInformation(
                        "Guest vehicle created: {VehicleId} ({VehicleName} - {LicensePlate}) for residence {ResidenceId}",
                        vehicle.VehicleId, vehicle.VehicleName, vehicle.VehicleLicensePlateNumber, residence.Id);
                }

                var guestId = "guest_" + Guid.NewGuid().ToString("N");

                var guest = new Guest
                {
                    GuestId = guestId,
                    GuestName = dto.GuestName,
                    GuestPhoneNumber = dto.GuestPhoneNumber,
                    Gender = dto.Gender ?? "",
                    Eta = dto.Eta,
                    CheckoutTime = dto.CheckoutTime ?? DateTime.UtcNow.AddDays(1),
                    ActualArrivalTime = null,
                    Status = "pending",
                    Userid = residentUserId,
                    ResidenceId = dto.ResidenceId,
                    VehicleId = vehicleId,  // Will be null if no vehicle details provided
                    //QrCode = string.Empty,  // Will be set by GenerateQRCode
                    IsVerified = false,
                    VisitCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Generate QR Code
                guest.QrCode = GenerateQRCode(guestId);

                await _context.Guests.AddAsync(guest);
                await _context.SaveChangesAsync();

                // Calculate validity duration
                var validityDuration = dto.CheckoutTime - dto.Eta;

                _logger.LogInformation(
                    "Guest registered: {GuestId} by {UserId}. Valid from {ETA} to {CheckoutTime} ({Duration} hours)",
                    guestId, residentUserId, dto.Eta, dto.CheckoutTime, validityDuration?.TotalHours);

                return Ok(new
                {
                    message = "Guest registered successfully",
                    guestId = guest.GuestId,
                    guestName = guest.GuestName,
                    qrCode = guest.QrCode,
                    eta = guest.Eta,
                    checkoutTime = guest.CheckoutTime,
                    status = guest.Status,
                    validityWindow = $"{guest.Eta:HH:mm} - {guest.CheckoutTime:HH:mm}",
                    validityDuration = $"{validityDuration?.TotalHours:F1} hours"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering guest");
                return StatusCode(500, new { message = "Error registering guest", error = ex.Message });
            }
        }

        // ============================================
        // POST: api/Guests/verify-qr/{guestId} (IoT/Security scans QR)
        // ============================================
        [HttpPost("verify-qr/{guestId}")]
        public async Task<IActionResult> VerifyQRCode(string guestId)
        {
            try
            {
                var guest = await _context.Guests
                    .Include(g => g.User)
                    .Include(g => g.Residence)
                    .FirstOrDefaultAsync(g => g.GuestId == guestId);

                if (guest == null)
                {
                    _logger.LogWarning("QR verification failed - Guest not found: {GuestId}", guestId);
                    return NotFound(new
                    {
                        success = false,
                        message = "Invalid QR code - Guest not found"
                    });
                }

                var now = DateTime.UtcNow;

                // ✅ CHECK 1: If checkout time passed, auto-mark as completed
                if (now > guest.CheckoutTime)
                {
                    if (guest.Status != "completed")
                    {
                        guest.Status = "completed";
                        guest.VisitCompleted = true;
                        guest.UpdatedAt = now;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Auto-completed expired guest: {GuestId}", guestId);
                    }

                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ QR code has expired",
                        checkoutTime = guest.CheckoutTime,
                        currentTime = now,
                        expiredSince = $"{(now - guest.CheckoutTime).TotalMinutes:F0} minutes ago"
                    });
                }

                // ✅ CHECK 2: Guest arrived too early (more than 2 hours early)
                var earlyArrivalWindow = TimeSpan.FromHours(23);
                var earliestAllowedTime = guest.Eta - earlyArrivalWindow;
                
                if (now < earliestAllowedTime)
                {
                    var minutesTooEarly = (earliestAllowedTime - now).TotalMinutes;
                    _logger.LogWarning("Guest {GuestId} arrived {Minutes} minutes too early (outside 2-hour window)", guestId, minutesTooEarly);
                    return BadRequest(new
                    {
                        success = false,
                        message = $"⏰ Guest is too early",
                        hint = $"You can arrive from {earliestAllowedTime:HH:mm} onwards (up to 2 hours before ETA)",
                        earliestAllowedTime = earliestAllowedTime,
                        eta = guest.Eta,
                        currentTime = now,
                        minutesUntilEarliestEntry = minutesTooEarly
                    });
                }

                // ✅ CHECK 3: Already completed (manual completion or cancelled)
                if (guest.Status == "completed")
                {
                    _logger.LogWarning("Guest {GuestId} tried to enter with completed status", guestId);
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ Visit has been completed or cancelled",
                        hint = "Please register a new visit"
                    });
                }

                // ✅ CHECK 4: If first time entering, record arrival time
                bool isFirstEntry = guest.ActualArrivalTime == null;

                if (isFirstEntry)
                {
                    guest.ActualArrivalTime = now;
                    _logger.LogInformation("✅ First entry - Guest {GuestId} arrived at {Time}", guestId, now);
                }
                else
                {
                    _logger.LogInformation("✅ Re-entry - Guest {GuestId} entering again at {Time}", guestId, now);
                }

                // ✅ Always set/update status to active (allows re-entry)
                guest.Status = "active";
                guest.IsVerified = true;
                guest.UpdatedAt = now;

                await _context.SaveChangesAsync();

                // Calculate time remaining
                var timeRemaining = guest.CheckoutTime - now;

                return Ok(new
                {
                    success = true,
                    message = isFirstEntry
                        ? "✅ Access granted - Welcome!"
                        : "✅ Access granted - Re-entry allowed",
                    entryType = isFirstEntry ? "first_entry" : "re_entry",
                    guestName = guest.GuestName,
                    guestPhone = guest.GuestPhoneNumber,
                    residentName = $"{guest.User?.Firstname} {guest.User?.Lastname}",
                    residence = $"{guest.Residence?.Block} - {guest.Residence?.FlatNumber}",
                    firstArrivalTime = guest.ActualArrivalTime,
                    currentTime = now,
                    validUntil = guest.CheckoutTime,
                    timeRemaining = $"{timeRemaining.TotalHours:F1} hours",
                    status = guest.Status,
                    isLate = isFirstEntry && now > guest.Eta
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying QR code for guest: {GuestId}", guestId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "System error",
                    error = ex.Message
                });
            }
        }

        // ============================================
        // PUT: api/Guests/{guestId}/extend-time (Extend guest checkout time)
        // ============================================
        [HttpPut("{guestId}/extend-time")]
        [Authorize(Roles = "resident")]
        public async Task<IActionResult> ExtendCheckoutTime(string guestId, [FromBody] ExtendGuestTimeDto dto)
        {
            try
            {
                var residentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(residentUserId))
                    return Unauthorized(new { message = "User not authenticated" });

                // Find guest and verify ownership
                var guest = await _context.Guests
                    .FirstOrDefaultAsync(g => g.GuestId == guestId && g.Userid == residentUserId);

                if (guest == null)
                    return NotFound(new { message = "Guest not found or you don't have permission" });

                // ✅ VALIDATION: Can't extend completed visit
                if (guest.Status == "completed")
                {
                    return BadRequest(new { 
                        message = "Cannot extend time for completed visit",
                        currentStatus = guest.Status
                    });
                }

                // ✅ VALIDATION: New checkout time must be in future
                if (dto.NewCheckoutTime <= DateTime.UtcNow)
                {
                    return BadRequest(new { 
                        message = "New checkout time must be in the future",
                        newCheckoutTime = dto.NewCheckoutTime,
                        currentTime = DateTime.UtcNow
                    });
                }

                // ✅ VALIDATION: New checkout time must be after current checkout time (actual extension)
                if (dto.NewCheckoutTime <= guest.CheckoutTime)
                {
                    return BadRequest(new { 
                        message = "New checkout time must be later than current checkout time",
                        currentCheckoutTime = guest.CheckoutTime,
                        newCheckoutTime = dto.NewCheckoutTime
                    });
                }

                var oldCheckoutTime = guest.CheckoutTime;
                var extensionDuration = dto.NewCheckoutTime - oldCheckoutTime;

                // ✅ Update checkout time (QR code remains same)
                guest.CheckoutTime = dto.NewCheckoutTime;
                guest.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Guest {GuestId} checkout time extended by {Hours} hours. Old: {OldTime}, New: {NewTime}",
                    guestId, extensionDuration.TotalHours, oldCheckoutTime, dto.NewCheckoutTime);

                return Ok(new
                {
                    message = "✅ Guest checkout time extended successfully",
                    guestId = guest.GuestId,
                    guestName = guest.GuestName,
                    oldCheckoutTime = oldCheckoutTime,
                    newCheckoutTime = guest.CheckoutTime,
                    extensionDuration = $"{extensionDuration.TotalHours:F1} hours",
                    validityWindow = $"{guest.Eta:HH:mm} - {guest.CheckoutTime:HH:mm}",
                    status = guest.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending guest checkout time: {GuestId}", guestId);
                return StatusCode(500, new { message = "Error extending checkout time", error = ex.Message });
            }
        }

        // ============================================
        // GET: api/Guests/verified (Get verified/active guests for dashboard)
        // ============================================
        [HttpGet("verified")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetVerifiedGuests()
        {
            try
            {
                // Auto-complete expired guests first
                await AutoCompleteExpiredGuests();

                var now = DateTime.UtcNow;

                // Get ONLY verified guests who are currently active
                var verifiedGuests = await _context.Guests
                    .Include(g => g.User)
                    .Include(g => g.Residence)
                    .Include(g => g.Vehicle)
                    .Where(g => 
                        g.IsVerified == true &&          // ✅ ONLY QR scanned guests
                        g.Status == "active" &&           // ✅ Currently active
                        g.CheckoutTime > now)             // ✅ Not expired
                    .OrderByDescending(g => g.ActualArrivalTime)
                    .Select(g => new
                    {
                        g.GuestId,
                        g.GuestName,
                        g.GuestPhoneNumber,
                        g.Gender,
                        
                        // Resident info
                        ResidentName = (g.User != null) 
                            ? $"{g.User.Firstname} {g.User.Lastname}" 
                            : "Unknown",
                        ResidentEmail = g.User != null ? g.User.Email : "",
                        ResidentPhone = g.User != null ? g.User.Phone : "",
                        
                        // Residence info
                        Residence = g.Residence != null ? new
                        {
                            g.Residence.Block,
                            FlatNumber = g.Residence.FlatNumber,
                            Address = g.Residence.AddressLine1
                        } : null,
                        
                        // Vehicle info (if any)
                        Vehicle = g.Vehicle != null ? new
                        {
                            g.Vehicle.VehicleName,
                            g.Vehicle.VehicleModel,
                            g.Vehicle.VehicleType,
                            g.Vehicle.VehicleLicensePlateNumber,
                            g.Vehicle.VehicleColor
                        } : null,
                        
                        // Timing details
                        ExpectedArrival = g.Eta,
                        ActualArrival = g.ActualArrivalTime,
                        CheckoutTime = g.CheckoutTime,
                        IsLate = g.ActualArrivalTime > g.Eta,
                        MinutesLate = g.ActualArrivalTime > g.Eta 
                            ? (g.ActualArrivalTime.Value - g.Eta).TotalMinutes 
                            : 0,
                        
                        // Time remaining (raw value - we'll format after)
                        TimeRemainingHours = (g.CheckoutTime - now).TotalHours,
                        
                        // Status
                        g.Status,
                        g.IsVerified,
                        g.CreatedAt,
                        VerifiedAt = g.ActualArrivalTime
                    })
                    .ToListAsync();

                // ✅ NOW format time remaining AFTER fetching data
                var result = verifiedGuests.Select(g => new
                {
                    g.GuestId,
                    g.GuestName,
                    g.GuestPhoneNumber,
                    g.Gender,
                    g.ResidentName,
                    g.ResidentEmail,
                    g.ResidentPhone,
                    g.Residence,
                    g.Vehicle,
                    g.ExpectedArrival,
                    g.ActualArrival,
                    g.CheckoutTime,
                    g.IsLate,
                    g.MinutesLate,
                    g.TimeRemainingHours,
                    TimeRemainingFormatted = FormatTimeRemaining(TimeSpan.FromHours(g.TimeRemainingHours)),
                    g.Status,
                    g.IsVerified,
                    g.CreatedAt,
                    g.VerifiedAt
                }).ToList();

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    currentTime = now,
                    guests = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving verified guests");
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Error retrieving verified guests", 
                    error = ex.Message 
                });
            }
        }

        // ============================================
        // GET: api/Guests/active-count (Quick count for dashboard stats)
        // ============================================
        [HttpGet("active-count")]
        [Authorize(Roles = "admin, employee")]
        public async Task<IActionResult> GetActiveGuestCount()
        {
            try
            {
                await AutoCompleteExpiredGuests();
                
                var now = DateTime.UtcNow;
                
                // Count ONLY verified active guests
                var activeCount = await _context.Guests
                    .CountAsync(g => 
                        g.Status == "active" && 
                        g.IsVerified == true &&
                        g.CheckoutTime > now);

                return Ok(new
                {
                    activeGuests = activeCount,
                    currentTime = now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active guest count");
                return StatusCode(500, new { message = "Error", error = ex.Message });
            }
        }

        // ============================================
        // HELPER: Auto-complete expired guests
        // ============================================
        private async Task<int> AutoCompleteExpiredGuests()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Find guests whose checkout time has passed
                var expiredGuests = await _context.Guests
                    .Where(g =>
                        (g.Status == "pending" || g.Status == "active") &&
                        g.CheckoutTime < now)
                    .ToListAsync();

                if (expiredGuests.Any())
                {
                    foreach (var guest in expiredGuests)
                    {
                        guest.Status = "completed";
                        guest.VisitCompleted = true;
                        guest.UpdatedAt = now;

                        _logger.LogInformation(
                            "Auto-completed visit for guest {GuestName} (ID: {GuestId}). CheckoutTime was {CheckoutTime}",
                            guest.GuestName,
                            guest.GuestId,
                            guest.CheckoutTime);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "✅ Auto-completed {Count} expired visits at {Time}",
                        expiredGuests.Count,
                        now);

                    return expiredGuests.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoCompleteExpiredGuests");
                return 0;
            }
        }

        // ============================================
        // HELPER: Generate QR Code
        // ============================================
        private byte[] GenerateQRCode(string guestId)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(guestId, QRCodeGenerator.ECCLevel.Q);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                return qrCodeBytes;
            }
        }

        // ============================================
        // HELPER: Format time remaining
        // ============================================
        private static string FormatTimeRemaining(TimeSpan timeSpan)
        {
            if (timeSpan.TotalMinutes < 0)
                return "Expired";
            
            if (timeSpan.TotalHours < 1)
                return $"{timeSpan.TotalMinutes:F0} minutes";
            
            if (timeSpan.TotalHours < 24)
                return $"{timeSpan.TotalHours:F1} hours";
            
            return $"{timeSpan.TotalDays:F1} days";
        }

        /// <summary>
        /// Get currently verified (active) guests for a specific residence (Admin/Employee only)
        /// Returns guests who are currently inside the society with their vehicle info
        /// </summary>
        [HttpGet("residence/{residenceId}/verified")]
        [Authorize(Roles = "admin,employee")]
        public async Task<IActionResult> GetVerifiedGuestsByResidence(Guid residenceId)
        {
            // Verify residence exists
            var residenceExists = await _context.Residences.AnyAsync(r => r.Id == residenceId);
            if (!residenceExists)
            {
                return NotFound(new { message = "Residence not found" });
            }

            var now = DateTime.UtcNow;

            var guests = await _context.Guests
                .Include(g => g.Vehicle)  // Include vehicle navigation property
                .Where(g =>
                    g.ResidenceId == residenceId &&
                    g.IsVerified == true &&
                    g.Status == "active" &&
                    g.CheckoutTime > now &&
                    g.VisitCompleted == false)
                .ToListAsync();

            // Map to response DTO with calculated fields
            var guestDtos = guests.Select(g => new
            {
                guestId = g.GuestId,
                guestName = g.GuestName,
                guestPhoneNumber = g.GuestPhoneNumber,
                gender = g.Gender,
                eta = g.Eta,
                checkoutTime = g.CheckoutTime,
                actualArrivalTime = g.ActualArrivalTime,
                status = g.Status,
                isVerified = g.IsVerified,
                
                // Timing calculations
                isLate = g.ActualArrivalTime.HasValue && g.ActualArrivalTime.Value > g.Eta,
                minutesLate = g.ActualArrivalTime.HasValue && g.ActualArrivalTime.Value > g.Eta
                    ? (g.ActualArrivalTime.Value - g.Eta).TotalMinutes
                    : 0,
                timeRemainingHours = (g.CheckoutTime - now).TotalHours,
                timeRemainingFormatted = $"{Math.Round((g.CheckoutTime - now).TotalHours, 1)} hours",
                
                // Vehicle info (directly embedded, null if no vehicle)
                vehicle = g.Vehicle == null ? null : new
                {
                    vehicleId = g.Vehicle.VehicleId,
                    vehicleName = g.Vehicle.VehicleName,
                    vehicleModel = g.Vehicle.VehicleModel,
                    vehicleType = g.Vehicle.VehicleType,
                    vehicleLicensePlateNumber = g.Vehicle.VehicleLicensePlateNumber,
                    vehicleRFIDTagId = g.Vehicle.VehicleRFIDTagId,
                    vehicleColor = g.Vehicle.VehicleColor
                }
            }).ToList();

            return Ok(new
            {
                success = true,
                count = guestDtos.Count,
                residenceId = residenceId,
                currentTime = now,
                guests = guestDtos
            });
        }

        /// <summary>
        /// Get guests for a specific residence
        /// </summary>
        [HttpGet("residence/{residenceId}")]
        public async Task<IActionResult> GetGuestsByResidence(Guid residenceId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found" });

            // Verify residence belongs to user
            var residence = await _context.Residences
                .FirstOrDefaultAsync(r => r.Id == residenceId && r.Userid == userId);

            if (residence == null)
                return NotFound(new { message = "Residence not found or does not belong to you." });

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
                    g.QrCode,
                    g.IsVerified,
                    g.VisitCompleted,
                    g.CreatedAt,
                    IsExpired = g.CheckoutTime < DateTime.UtcNow,
                    IsLate = g.Eta < DateTime.UtcNow && g.ActualArrivalTime == null,
                    TimeRemaining = (g.CheckoutTime - DateTime.UtcNow).TotalHours > 0 
                        ? $"{(int)(g.CheckoutTime - DateTime.UtcNow).TotalHours}h remaining"
                        : "Expired"
                })
                .ToListAsync();

            return Ok(guests);
        }
    }
}

