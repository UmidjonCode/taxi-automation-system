using HotelOS.Reception.Services;

namespace HotelOS.Reception.BackgroundServices;

/// <summary>
/// Runs every 30 seconds and expires any room holds that have passed their ExpiresAt time.
/// This ensures rooms are released back to availability if the guest doesn't complete payment.
/// </summary>
public sealed class HoldExpiryService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HoldExpiryService> _logger;

    /// <summary>How often to scan for expired holds.</summary>
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(30);

    public HoldExpiryService(IServiceProvider services, ILogger<HoldExpiryService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HoldExpiryService started. Scanning every {Interval}s.", ScanInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var facade = scope.ServiceProvider.GetRequiredService<ReceptionFacade>();
                await facade.ExpireStaleHoldsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring stale holds.");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }
    }
}
