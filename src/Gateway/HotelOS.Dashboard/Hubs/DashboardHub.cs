using Microsoft.AspNetCore.SignalR;

namespace HotelOS.Dashboard.Hubs;

/// <summary>
/// The WebSocket endpoint browsers connect to. It is push-only: the server
/// streams hotel events to every connected client via <c>Clients.All</c>.
/// </summary>
public sealed class DashboardHub : Hub
{
}
