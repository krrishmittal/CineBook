using CineBook.Application.Interfaces;
using CineBook.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CineBook.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateToken(ApplicationUser user, string role)
        {
            var keyString = _config["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(keyString))
            {
                _logger.LogError("GenerateToken failed: JwtSettings:SecretKey is not configured.");
                throw new InvalidOperationException("JWT Secret Key is missing.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new Claim(ClaimTypes.Role, role),
                new Claim("FullName", user.FullName ?? string.Empty)
            };

            var expiryMinutesStr = _config["JwtSettings:AccessTokenExpiryMinutes"] ?? "60";
            if (!double.TryParse(expiryMinutesStr, out double expiryMinutes))
            {
                expiryMinutes = 60;
                _logger.LogWarning("Invalid JwtSettings:AccessTokenExpiryMinutes value. Defaulting to 60 minutes.");
            }

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"],
                audience: _config["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha512)
            );

            _logger.LogInformation("Generated JWT token successfully for user {UserId} with role '{Role}'", user.Id, role);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            _logger.LogInformation("Generated a new refresh token");

            return Convert.ToBase64String(randomBytes);
        }
    }
}