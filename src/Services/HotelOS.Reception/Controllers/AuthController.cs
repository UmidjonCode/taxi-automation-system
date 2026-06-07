using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using HotelOS.Reception.Data;
using HotelOS.Reception.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HotelOS.Reception.Controllers;

public record RegisterRequest(string FullName, string Email, string PhoneNumber, string? NationalId, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, Guid AccountId, Guid? GuestId, string Email, string FullName, string Role);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ReceptionDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(ReceptionDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        // ── Validation ──────────────────────────────────────────
        if (!Regex.IsMatch(req.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            return BadRequest(new { error = "Invalid email format." });

        if (req.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters." });

        if (!req.Password.Any(char.IsDigit))
            return BadRequest(new { error = "Password must contain at least 1 digit." });

        if (!req.Password.Any(char.IsUpper))
            return BadRequest(new { error = "Password must contain at least 1 uppercase letter." });

        if (await _db.Accounts.AnyAsync(a => a.Email == req.Email, ct))
            return BadRequest(new { error = "Email already registered." });

        // ── Create Guest + Account in a transaction ─────────────
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var guest = new Guest
            {
                FullName = req.FullName,
                Email = req.Email,
                PhoneNumber = req.PhoneNumber,
                NationalId = req.NationalId
            };
            _db.Guests.Add(guest);
            await _db.SaveChangesAsync(ct);

            var account = new Account
            {
                Email = req.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                Role = AccountRole.Guest,
                GuestId = guest.Id
            };
            _db.Accounts.Add(account);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var token = GenerateJwt(account, guest.FullName);
            return Ok(new AuthResponse(token, account.Id, guest.Id, account.Email, guest.FullName, account.Role.ToString()));
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var account = await _db.Accounts
            .Include(a => a.Guest)
            .FirstOrDefaultAsync(a => a.Email == req.Email, ct);

        if (account == null || !BCrypt.Net.BCrypt.Verify(req.Password, account.PasswordHash))
            return Unauthorized(new { error = "Invalid email or password." });

        var fullName = account.Guest?.FullName ?? "Staff";
        var token = GenerateJwt(account, fullName);
        return Ok(new AuthResponse(token, account.Id, account.GuestId, account.Email, fullName, account.Role.ToString()));
    }

    // ─── JWT Helper ────────────────────────────────────────────

    private string GenerateJwt(Account account, string fullName)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "HotelOS-SuperSecret-Key-Change-In-Production-2026!"));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(ClaimTypes.Role, account.Role.ToString()),
            new("fullName", fullName)
        };

        if (account.GuestId.HasValue)
            claims.Add(new Claim("guestId", account.GuestId.Value.ToString()));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "HotelOS",
            audience: _config["Jwt:Audience"] ?? "HotelOS.Web",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
