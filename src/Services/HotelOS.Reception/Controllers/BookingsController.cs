using HotelOS.Reception.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Reception.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly ReceptionFacade _facade;
    public BookingsController(ReceptionFacade facade) => _facade = facade;

    [HttpGet("my")]
    public Task<IActionResult> GetMyBookings([FromQuery] string email, CancellationToken ct)
        => Guard(async () => (object)await _facade.GetBookingsByEmailAsync(email, ct));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateBookingRequest req, CancellationToken ct)
        => Guard(() => _facade.CreateBookingAsync(req, ct));

    // ─── Room Hold Endpoints ───────────────────────────────────

    /// <summary>Hold a room for 5 minutes while the guest completes payment.</summary>
    [HttpPost("hold")]
    public Task<IActionResult> CreateHold([FromBody] CreateHoldRequest req, CancellationToken ct)
        => Guard(async () =>
        {
            var hold = await _facade.CreateHoldAsync(req.RoomId, req.GuestId, req.CheckIn, req.CheckOut, ct);
            return (object)new
            {
                hold.Id,
                hold.RoomId,
                hold.GuestId,
                hold.CheckIn,
                hold.CheckOut,
                hold.ExpiresAt,
                Status = hold.Status.ToString()
            };
        });

    /// <summary>Convert a hold into a confirmed booking (payment received).</summary>
    [HttpPost("hold/{id:guid}/confirm")]
    public Task<IActionResult> ConfirmHold(Guid id, [FromBody] ConfirmHoldRequest req, CancellationToken ct)
        => Guard(() => _facade.ConfirmHoldAsync(id, req.AdvancePayment, ct));

    /// <summary>Manually release a hold (guest cancelled the payment).</summary>
    [HttpDelete("hold/{id:guid}")]
    public Task<IActionResult> ReleaseHold(Guid id, CancellationToken ct)
        => Guard(async () =>
        {
            await _facade.ReleaseHoldAsync(id, ct);
            return (object)new { ok = true };
        });

    // ─── Booking Lifecycle ─────────────────────────────────────

    [HttpPost("{id:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmBookingRequest req, CancellationToken ct)
        => Guard(() => _facade.ConfirmBookingAsync(id, req.AdvancePayment, ct));

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        => Guard(() => _facade.CancelBookingAsync(id, ct));

    [HttpPost("{id:guid}/checkin")]
    public Task<IActionResult> CheckIn(Guid id, CancellationToken ct)
        => Guard(() => _facade.CheckInAsync(id, ct));

    [HttpPost("{id:guid}/checkout")]
    public Task<IActionResult> CheckOut(Guid id, CancellationToken ct)
        => Guard(() => _facade.CheckOutAsync(id, ct));

    [HttpPost("{id:guid}/extras")]
    public Task<IActionResult> AddExtra(Guid id, [FromBody] AddExtraRequest req, CancellationToken ct)
        => Guard(async () =>
        {
            await _facade.AddExtraChargeAsync(id, req.Description, req.Amount, ct);
            return (object)new { ok = true };
        });

    /// <summary>Maps domain exceptions to HTTP results so controllers stay thin.</summary>
    private async Task<IActionResult> Guard<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
