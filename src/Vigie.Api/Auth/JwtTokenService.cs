using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Vigie.Domain;

namespace Vigie.Api.Auth;

public sealed class JwtTokenService(string key, TimeSpan lifetime)
{
    public (string Token, DateTimeOffset ExpiresAtUtc) Create(Employee employee, OrganizationMembership? membership = null)
    {
        var expires = DateTimeOffset.UtcNow.Add(lifetime);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var role = membership?.Role ?? employee.Role;
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.Name),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("organization_id", employee.OrganizationId.ToString()),
            new Claim("session_version", employee.SessionVersion.ToString(CultureInfo.InvariantCulture))
        };
        if (employee.Role != role) claims.Add(new Claim(ClaimTypes.Role, employee.Role.ToString()));
        if (role is EmployeeRole.PoolChief or EmployeeRole.SectorManager or EmployeeRole.AquaticDirector)
            claims.Add(new Claim(ClaimTypes.Role, nameof(EmployeeRole.Coordinator)));
        if (membership is not null)
        {
            claims.Add(new Claim("membership_id", membership.Id.ToString()));
            if (membership.SiteId.HasValue) claims.Add(new Claim("site_id", membership.SiteId.Value.ToString()));
            if (membership.SectorId.HasValue) claims.Add(new Claim("sector_id", membership.SectorId.Value.ToString()));
        }
        var token = new JwtSecurityToken(claims: claims, expires: expires.UtcDateTime, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
