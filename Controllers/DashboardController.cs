using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace VaultX_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,employee")]
    public class DashboardController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public DashboardController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetDashboardData()
        {
            var todaysDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var totalUsers = await _context.Users.CountAsync();
            var totalGuests = await _context.Guests.CountAsync();
            var totalVehicles = await _context.Vehicles.CountAsync();
            var pendingResidents = await _context.Residences.CountAsync(r => !r.IsApprovedBySociety);
            var approvedResidents = await _context.Residences.CountAsync(r => r.IsApprovedBySociety);

            var data = new DashboardDataDto
            {
                TodaysDate = todaysDate,
                TotalUsers = totalUsers,
                TotalGuests = totalGuests,
                TotalVehicles = totalVehicles,
                PendingResidents = pendingResidents,
                ApprovedResidents = approvedResidents
            };

            return Ok(data);
        }
    }
}
