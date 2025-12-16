using BCrypt.Net;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VaultX_WebAPI.DTOs;
using VaultX_WebAPI.Models;

namespace VaultX_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly VaultxDbContext _context;

        private readonly IConfiguration _config;

        public AuthController(VaultxDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                return BadRequest(new { Message = "Email is already registered." });
            }

            var user = new User
            {
                Userid = Guid.NewGuid().ToString(),
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "resident",
                IsEmailVerified = false,
                IsVerified = false,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var otp = new Random().Next(100000, 999999).ToString();
            var otpRecord = new Otp
            {
                Code = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                UserUserid = user.Userid,
                UserUser = user
            };

            try
            {
                
                _context.Users.Add(user);
                _context.Otps.Add(otpRecord);
                await _context.SaveChangesAsync();

                
                await SendOtpEmail(user.Email, otp);

                return Ok(new { Message = "User registered. Please verify your email with the OTP sent.", UserId = user.Userid });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to register user or send OTP.", Error = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpRequestDto dto)
        {
            var otpRecord = await _context.Otps
                .Include(o => o.UserUser)
                .FirstOrDefaultAsync(o => o.UserUser.Email == dto.Email && o.Code == dto.Otp && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

            if (otpRecord == null)
            {
                return BadRequest(new OtpResponseDto { Success = false, Message = "Invalid or expired OTP." });
            }

            try
            {
                otpRecord.IsUsed = true;
                otpRecord.UserUser.IsEmailVerified = true;
                otpRecord.UserUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new OtpResponseDto { Success = true, Message = "Email verified successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OtpResponseDto { Success = false, Message = "Failed to verify OTP.", Error = ex.Message });
            }
        }

        private async Task SendOtpEmail(string email, string otp)
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"]);
            var smtpUser = _config["Smtp:Username"];
            var smtpPass = _config["Smtp:Password"];

            var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser, "VaultX"),
                Subject = "VaultX Email Verification OTP",
                Body = $"Your OTP for email verification is: {otp}. It is valid for 10 minutes.",
                IsBodyHtml = false,
            };
            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid model state." });
                }

                // ✅ Include Residences to get approval status
                var user = await _context.Users
                    .Include(u => u.Residences)
                    .SingleOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email" });
                }

                if (!VerifyPassword(model.Password, user.Password))
                    return Unauthorized(new { message = "Invalid password" });

                var accessTokenString = BuildAccessToken(user.Userid, user.Role);

                // ✅ Get approval status from primary residence
                var primaryResidence = user.Residences?.FirstOrDefault(r => r.IsPrimary);
                var isApprovedBySociety = primaryResidence?.IsApprovedBySociety ?? false;

                // ✅ Return BOTH token and approval status
                return Ok(new 
                { 
                    message = "Login successful.", 
                    accessToken = accessTokenString,  // ✅ lowercase 'a'
                    isApprovedBySociety = isApprovedBySociety  // ✅ Add approval flag
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred during login.", error = ex.Message });
            }
        }

        private static bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(plainPassword, storedHash);
            }
            catch
            {
                return false;
            }
        }

        private static string HashPassword(string plainPassword, int workFactor = 10)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor);
        }

        private string BuildAccessToken(string userId, string? role = null)
        {
            try
            {
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

                var creds = new SigningCredentials(
                    key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                };


                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Issuer"],
                    claims: claims,
                    notBefore: null,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds);

                return new JwtSecurityTokenHandler().WriteToken(token);

            }catch (Exception ex)
            {
                return $"Error generating access token: {ex.Message}";
            }
        }

        private string BuildRefreshToken()
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: new[] { new System.Security.Claims.Claim("type", "refresh") },
                notBefore: null,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool ValidateRefreshToken(string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_config["Jwt:Key"]!))
                };
                tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out SecurityToken validatedToken);
                var jwtToken = (JwtSecurityToken)validatedToken;
                return jwtToken?.Claims.Any(c => c.Type == "type" && c.Value == "refresh") ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}
