using HotelOS.RoomService.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.RoomService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly RoomServiceFacade _facade;
    public MenuController(RoomServiceFacade facade) => _facade = facade;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _facade.GetMenuAsync(ct));
}
