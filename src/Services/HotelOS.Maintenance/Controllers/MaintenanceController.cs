using HotelOS.Maintenance.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Maintenance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly MaintenanceFacade _facade;
    public MaintenanceController(MaintenanceFacade facade) => _facade = facade;

    /// <summary>Current backlog ordered Critical→Low, FIFO within a priority.</summary>
    [HttpGet("queue")]
    public IActionResult Queue() => Ok(_facade.GetQueueSnapshot());

    [HttpGet]
    public async Task<IActionResult> All(CancellationToken ct) => Ok(await _facade.GetAllAsync(ct));

    /// <summary>Take the highest-priority job and mark it in progress.</summary>
    [HttpPost("start-next")]
    public async Task<IActionResult> StartNext(CancellationToken ct)
    {
        var item = await _facade.StartNextAsync(ct);
        return item is null ? Ok(new { message = "Queue is empty." }) : Ok(item);
    }

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        try
        {
            await _facade.ResolveAsync(id, ct);
            return Ok(new { ok = true });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
