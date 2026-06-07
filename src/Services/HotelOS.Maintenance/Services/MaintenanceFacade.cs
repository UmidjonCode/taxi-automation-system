using HotelOS.Maintenance.Data;
using HotelOS.Maintenance.Models;
using HotelOS.Shared.Algorithms.Maintenance;
using HotelOS.Shared.Enums;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Maintenance.Services;

/// <summary>Facade over the maintenance subsystem: durable store + priority queue + events.</summary>
public sealed class MaintenanceFacade
{
    private readonly MaintenanceDbContext _db;
    private readonly IMaintenancePriorityQueue _queue;
    private readonly IEventBus _bus;
    private readonly ILogger<MaintenanceFacade> _logger;

    public MaintenanceFacade(
        MaintenanceDbContext db,
        IMaintenancePriorityQueue queue,
        IEventBus bus,
        ILogger<MaintenanceFacade> logger)
    {
        _db = db;
        _queue = queue;
        _bus = bus;
        _logger = logger;
    }

    /// <summary>Persist a reported fault and enqueue it by priority.</summary>
    public async Task IntakeAsync(RoomMaintenanceRequestedEvent e, CancellationToken ct = default)
    {
        var req = new MaintenanceRequest
        {
            RoomId = e.RoomId,
            RoomNumber = e.RoomNumber,
            Description = e.Description,
            Priority = e.Priority,
            ReportedBy = e.ReportedBy,
            SubmittedAt = e.SubmittedAt,
            Status = MaintenanceStatus.Open
        };
        _db.MaintenanceRequests.Add(req);
        await _db.SaveChangesAsync(ct);

        _queue.Enqueue(ToItem(req));

        await _bus.PublishAsync(new MaintenanceStatusUpdatedEvent
        {
            RequestId = req.Id,
            RoomId = req.RoomId,
            Status = MaintenanceStatus.Open,
            UpdatedAt = DateTime.UtcNow
        }, ct);

        _logger.LogInformation("Maintenance {Id} enqueued [{Priority}] for room {Room}.", req.Id, req.Priority, req.RoomNumber);
    }

    public IReadOnlyList<MaintenanceRequestItem> GetQueueSnapshot() => _queue.Snapshot();

    public Task<List<MaintenanceRequest>> GetAllAsync(CancellationToken ct = default) =>
        _db.MaintenanceRequests.AsNoTracking().OrderByDescending(r => r.SubmittedAt).ToListAsync(ct);

    /// <summary>Pop the highest-priority job and mark it InProgress.</summary>
    public async Task<MaintenanceRequestItem?> StartNextAsync(CancellationToken ct = default)
    {
        if (!_queue.TryDequeue(out var item)) return null;

        var req = await _db.MaintenanceRequests.FirstOrDefaultAsync(r => r.Id == item.RequestId, ct);
        if (req is not null)
        {
            req.Status = MaintenanceStatus.InProgress;
            await _db.SaveChangesAsync(ct);
            await _bus.PublishAsync(new MaintenanceStatusUpdatedEvent
            {
                RequestId = req.Id,
                RoomId = req.RoomId,
                Status = MaintenanceStatus.InProgress,
                UpdatedAt = DateTime.UtcNow
            }, ct);
            _logger.LogInformation("Maintenance {Id} started (room {Room}, {Priority}).", req.Id, req.RoomNumber, req.Priority);
        }
        return item;
    }

    public async Task ResolveAsync(Guid requestId, CancellationToken ct = default)
    {
        var req = await _db.MaintenanceRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new KeyNotFoundException($"Maintenance request {requestId} not found.");
        if (req.Status == MaintenanceStatus.Resolved)
            throw new InvalidOperationException("Request already resolved.");

        req.Status = MaintenanceStatus.Resolved;
        req.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new MaintenanceStatusUpdatedEvent
        {
            RequestId = req.Id,
            RoomId = req.RoomId,
            Status = MaintenanceStatus.Resolved,
            UpdatedAt = DateTime.UtcNow
        }, ct);
        await _bus.PublishAsync(new MaintenanceResolvedEvent
        {
            RequestId = req.Id,
            RoomId = req.RoomId,
            ResolvedAt = req.ResolvedAt.Value
        }, ct);

        _logger.LogInformation("Maintenance {Id} resolved (room {Room}).", req.Id, req.RoomNumber);
    }

    /// <summary>On startup, refill the in-memory queue from any still-Open rows.</summary>
    public async Task RehydrateQueueAsync(CancellationToken ct = default)
    {
        var open = await _db.MaintenanceRequests.AsNoTracking()
            .Where(r => r.Status == MaintenanceStatus.Open)
            .OrderBy(r => r.Priority).ThenBy(r => r.SubmittedAt)
            .ToListAsync(ct);

        foreach (var r in open) _queue.Enqueue(ToItem(r));
        if (open.Count > 0) _logger.LogInformation("Rehydrated {Count} open maintenance item(s) into the queue.", open.Count);
    }

    private static MaintenanceRequestItem ToItem(MaintenanceRequest r) => new()
    {
        RequestId = r.Id,
        RoomId = r.RoomId,
        RoomNumber = r.RoomNumber,
        Description = r.Description,
        Priority = r.Priority,
        SubmittedAt = r.SubmittedAt
    };
}
