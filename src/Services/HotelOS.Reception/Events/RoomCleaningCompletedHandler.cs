using HotelOS.Reception.Data;
using HotelOS.Shared.Enums;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Reception.Events;

/// <summary>When Housekeeping finishes, flip the room back to Clean/Available so it can be re-assigned.</summary>
public sealed class RoomCleaningCompletedHandler : IIntegrationEventHandler<RoomCleaningCompletedEvent>
{
    private readonly ReceptionDbContext _db;
    private readonly ILogger<RoomCleaningCompletedHandler> _logger;

    public RoomCleaningCompletedHandler(ReceptionDbContext db, ILogger<RoomCleaningCompletedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(RoomCleaningCompletedEvent e, CancellationToken ct = default)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == e.RoomId, ct);
        if (room is null) return;

        room.CleanStatus = RoomCleanStatus.Clean;
        room.Status = RoomStatus.Available;
        room.LastCleanedAt = e.CompletedAt;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Room {Room} is Clean/Available (cleaning task {Task}).", room.RoomNumber, e.TaskId);
    }
}
