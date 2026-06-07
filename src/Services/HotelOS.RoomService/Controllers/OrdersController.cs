using HotelOS.RoomService.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.RoomService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly RoomServiceFacade _facade;
    public OrdersController(RoomServiceFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _facade.GetOrdersAsync(ct));

    [HttpPost]
    public Task<IActionResult> Place([FromBody] PlaceOrderRequest req, CancellationToken ct)
        => Guard(() => _facade.PlaceOrderAsync(req, ct));

    [HttpPost("{id:guid}/deliver")]
    public Task<IActionResult> Deliver(Guid id, CancellationToken ct)
        => Guard(() => _facade.MarkDeliveredAsync(id, ct));

    private async Task<IActionResult> Guard<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
