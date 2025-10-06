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

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid model state." });
                }

                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email" });
                }

                if (!VerifyPassword(model.Password, user.Password))
                    return Unauthorized("Invalid password");


                //var refreshTokenString = BuildRefreshToken();
                var accessTokenString = BuildAccessToken(user.Userid);
                //user.RefreshToken = refreshTokenString;
                //var updateResult = await _userManager.UpdateAsync(user);

                //if (!updateResult.Succeeded)
                //    return BadRequest(new { message = "Failed to update user with refresh token.", errors = updateResult.Errors.Select(e => e.Description) });


                var response = Ok(new { message = "Login successful.", AccessToken = accessTokenString });

                return response;

            }catch (Exception ex)
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

        private string BuildAccessToken(string userId)
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

                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Issuer"],
                    claims: claims,
                    notBefore: null,
                    expires: DateTime.Now.AddSeconds(10),
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
