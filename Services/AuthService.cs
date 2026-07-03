using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpCosmetiqueApi.DTOs;
using ErpCosmetiqueApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ErpCosmetiqueApi.Services;

public class AuthService(ErpDbContext db, IConfiguration config)
{
    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await db.Users
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        var token = GenerateToken(user);
        return new LoginResponseDto(
            token,
            user.Username,
            user.Role,
            user.EmployeeId,
            user.Employee?.Name);
    }

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await db.Users
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user is null) return null;
        return new UserProfileDto(user.Id, user.Username, user.Role, user.EmployeeId, user.Employee?.Name);
    }

    public static string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    private string GenerateToken(AppUser user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "ErpCosmetiqueSecretKey2026Minimum32Chars!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        if (user.EmployeeId.HasValue)
            claims.Add(new Claim("employeeId", user.EmployeeId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "ErpCosmetique",
            audience: config["Jwt:Audience"] ?? "ErpCosmetique",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
