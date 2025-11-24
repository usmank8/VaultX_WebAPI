using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]

    public class EmployeesController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        public EmployeesController(VaultxDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            try
            {
                // Validate: Check if email or CNIC already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email || u.Cnic == dto.Cnic);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email or CNIC already exists" });
                }

                // 1️⃣ CREATE USER RECORD
                var userId = "usr_" + Guid.NewGuid().ToString("N");
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Userid = userId,
                    Firstname = dto.Firstname,
                    Lastname = dto.Lastname,
                    Email = dto.Email,
                    Password = hashedPassword,
                    Phone = dto.Phone,
                    Cnic = dto.Cnic,
                    Role = "employee",
                    IsVerified = true,
                    IsEmailVerified = true,
                    IsBlocked = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // 2️⃣ CREATE EMPLOYEE RECORD
                var employee = new Employee
                {
                    Id = Guid.NewGuid(),
                    Userid = userId,  // Link to user
                    InternalRole = dto.InternalRole,
                    Department = dto.Department,
                    Shift = dto.Shift,
                    JoiningDate = dto.JoiningDate ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Employee created successfully",
                    employeeId = employee.Id.ToString(),
                    userId = userId,
                    email = user.Email,
                    fullName = $"{user.Firstname} {user.Lastname}",
                    internalRole = employee.InternalRole,
                    department = employee.Department,
                    shift = employee.Shift,
                    joiningDate = employee.JoiningDate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to create employee", error = ex.Message });
            }
        }

        [HttpGet("all")]
        [Authorize(Roles = "admin,employee")]
        public async Task<ActionResult<List<GetEmployeeProfileDto>>> GetAllEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.User)
                .ToListAsync();

            if (employees == null || employees.Count == 0)
            {
                return NotFound("No employees found.");
            }

            var dtos = employees.Select(e => new GetEmployeeProfileDto
            {
                EmployeeId = e.Id.ToString(),
                Firstname = e.User?.Firstname ?? string.Empty,
                Lastname = e.User?.Lastname ?? string.Empty,
                Email = e.User?.Email ?? string.Empty,
                Phone = e.User?.Phone,
                Cnic = e.User?.Cnic,
                InternalRole = e.InternalRole,
                Department = e.Department,
                Shift = e.Shift,
                JoiningDate = e.JoiningDate
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("profile/{employeeId}")]
        [Authorize(Roles = "admin,employee")]
        public async Task<ActionResult<GetEmployeeProfileDto>> GetEmployeeProfile(string employeeId)
        {
            var employee = await _context.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.Id.ToString() == employeeId);

            if (employee == null)
            {
                return NotFound("Employee profile not found.");
            }

            return Ok(new GetEmployeeProfileDto
            {
                EmployeeId = employee.Id.ToString(),
                Firstname = employee.User?.Firstname ?? string.Empty,
                Lastname = employee.User?.Lastname ?? string.Empty,
                Email = employee.User?.Email ?? string.Empty,
                Phone = employee.User?.Phone,
                Cnic = employee.User?.Cnic,
                InternalRole = employee.InternalRole,
                Department = employee.Department,
                Shift = employee.Shift,
                JoiningDate = employee.JoiningDate
            });
        }

    }
}
