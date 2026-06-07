using HotelOS.Reception.Services;
using HotelOS.Shared.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Reception.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly ReceptionFacade _facade;
    public RoomsController(ReceptionFacade facade) => _facade = facade;

    /// <summary>Search rooms that are clean, in service and free for the requested dates.</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] RoomStyle? style,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut,
        [FromQuery] Guid? branchId,
        CancellationToken ct)
    {
        var rooms = await _facade.SearchRoomsAsync(style, checkIn, checkOut, branchId, ct);
        return Ok(rooms.Select(r => new
        {
            r.Id,
            r.RoomNumber,
            r.Floor,
            Style = r.Style.ToString(),
            r.NightlyRate,
            r.ProximityZone
        }));
    }
}
