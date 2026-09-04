using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Vigie.Domain;

namespace Vigie.Api.Auth;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public (string Token, DateTimeOffset ExpiresAtUtc) Create(Employee employee)
    {
        var expires = DateTimeOffset.UtcNow.AddHours(4);
        var key = string.IsNullOrWhiteSpace(configuration["Jwt:Key"]) ? "vigie-development-key-change-in-production-2026" : configuration["Jwt:Key"]!;
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.Name),
            new Claim(ClaimTypes.Role, employee.Role.ToString())
        };
        var token = new JwtSecurityToken(claims: claims, expires: expires.UtcDateTime, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
