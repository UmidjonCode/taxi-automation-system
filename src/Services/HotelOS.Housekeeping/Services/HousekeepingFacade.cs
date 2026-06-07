using HotelOS.Housekeeping.Data;
using HotelOS.Housekeeping.Models;
using HotelOS.Shared.Enums;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Housekeeping.Services;

/// <summary>Facade over the housekeeping subsystem: cleaning tasks + maintenance reporting.</summary>
public sealed class HousekeepingFacade
{
    private readonly HousekeepingDbContext _db;
    private readonly IEventBus _bus;
    private readonly ILogger<HousekeepingFacade> _logger;

    public HousekeepingFacade(HousekeepingDbContext db, IEventBus bus, ILogger<HousekeepingFacade> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    public Task<List<CleaningTask>> GetTasksAsync(CancellationToken ct = default) =>
        _db.CleaningTasks.AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

    /// <summary>Called by the event handler when Reception requests a room cleaning.</summary>
    public async Task<CleaningTask> CreateTaskAsync(Guid roomId, string roomNumber, int floor, CancellationToken ct = default)
    {
        var existing = await _db.CleaningTasks
            .FirstOrDefaultAsync(t => t.RoomId == roomId && t.Status != CleaningTaskStatus.Done, ct);
        if (existing is not null) return existing; // idempotent against redelivered events

        var task = new CleaningTask { RoomId = roomId, RoomNumber = roomNumber, Floor = floor };
        _db.CleaningTasks.Add(task);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Cleaning task {Task} created for room {Room}.", task.Id, roomNumber);
        return task;
    }

    public async Task<CleaningTask> StartCleaningAsync(Guid taskId, Guid? housekeeperId, CancellationToken ct = default)
    {
        var task = await LoadAsync(taskId, ct);
        if (task.Status == CleaningTaskStatus.Done)
            throw new InvalidOperationException("Task already completed.");

        var assignee = housekeeperId ?? await FirstAvailableHousekeeperAsync(ct);
        task.HousekeeperId = assignee;
        task.Status = CleaningTaskStatus.InProgress;
        task.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new RoomCleaningStartedEvent
        {
            TaskId = task.Id,
            RoomId = task.RoomId,
            HousekeeperId = assignee,
            StartedAt = task.StartedAt.Value
        }, ct);

        return task;
    }

    public async Task<CleaningTask> CompleteCleaningAsync(Guid taskId, CancellationToken ct = default)
    {
        var task = await LoadAsync(taskId, ct);
        task.Status = CleaningTaskStatus.Done;
        task.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new RoomCleaningCompletedEvent
        {
            TaskId = task.Id,
            RoomId = task.RoomId,
            CompletedAt = task.CompletedAt.Value
        }, ct);

        _logger.LogInformation("Cleaning task {Task} for room {Room} completed.", task.Id, task.RoomNumber);
        return task;
    }

    /// <summary>Housekeeper reports a fault; Maintenance will enqueue it by priority.</summary>
    public async Task ReportMaintenanceAsync(ReportMaintenanceRequest req, CancellationToken ct = default)
    {
        await _bus.PublishAsync(new RoomMaintenanceRequestedEvent
        {
            RoomId = req.RoomId,
            RoomNumber = req.RoomNumber,
            Description = req.Description,
            Priority = req.Priority,
            ReportedBy = req.HousekeeperId,
            SubmittedAt = DateTime.UtcNow
        }, ct);

        _logger.LogInformation("Maintenance reported for room {Room} ({Priority}).", req.RoomNumber, req.Priority);
    }

    private async Task<CleaningTask> LoadAsync(Guid taskId, CancellationToken ct) =>
        await _db.CleaningTasks.FirstOrDefaultAsync(t => t.Id == taskId, ct)
        ?? throw new KeyNotFoundException($"Cleaning task {taskId} not found.");

    private async Task<Guid> FirstAvailableHousekeeperAsync(CancellationToken ct)
    {
        var hk = await _db.Housekeepers.FirstOrDefaultAsync(h => h.IsAvailable, ct);
        return hk?.Id ?? Guid.Empty;
    }
}
