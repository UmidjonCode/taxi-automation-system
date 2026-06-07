using HotelOS.Housekeeping.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.Housekeeping.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CleaningController : ControllerBase
{
    private readonly HousekeepingFacade _facade;
    public CleaningController(HousekeepingFacade facade) => _facade = facade;

    [HttpGet("tasks")]
    public async Task<IActionResult> Tasks(CancellationToken ct) => Ok(await _facade.GetTasksAsync(ct));

    [HttpPost("tasks/{id:guid}/start")]
    public Task<IActionResult> Start(Guid id, [FromBody] StartCleaningRequest req, CancellationToken ct)
        => Guard(() => _facade.StartCleaningAsync(id, req.HousekeeperId, ct));

    [HttpPost("tasks/{id:guid}/complete")]
    public Task<IActionResult> Complete(Guid id, CancellationToken ct)
        => Guard(() => _facade.CompleteCleaningAsync(id, ct));

    [HttpPost("maintenance-requests")]
    public Task<IActionResult> ReportMaintenance([FromBody] ReportMaintenanceRequest req, CancellationToken ct)
        => Guard(async () =>
        {
            await _facade.ReportMaintenanceAsync(req, ct);
            return (object)new { ok = true };
        });

    private async Task<IActionResult> Guard<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
