using System.Security.Cryptography;
using System.Text;
using HotelOS.Reception.Data;
using HotelOS.Reception.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Reception.Controllers;

public record RegisterRequest(string FullName, string Email, string PhoneNumber, string? NationalId, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(Guid GuestId, string Email, string FullName);

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ReceptionDbContext _db;

    public AuthController(ReceptionDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        if (await _db.Guests.AnyAsync(g => g.Email == req.Email, ct))
            return BadRequest(new { error = "Email already registered." });

        var guest = new Guest
        {
            FullName = req.FullName,
            Email = req.Email,
            PhoneNumber = req.PhoneNumber,
            NationalId = req.NationalId,
            PasswordHash = HashPassword(req.Password)
        };

        _db.Guests.Add(guest);
        await _db.SaveChangesAsync(ct);

        return Ok(new AuthResponse(guest.Id, guest.Email, guest.FullName));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Email == req.Email, ct);
        if (guest == null) return Unauthorized(new { error = "Invalid email or password." });

        if (guest.PasswordHash != HashPassword(req.Password))
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(new AuthResponse(guest.Id, guest.Email, guest.FullName));
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
