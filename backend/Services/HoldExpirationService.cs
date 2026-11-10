using backend.Data;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class HoldExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpirationService> _logger;

    public HoldExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(2));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireHoldsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while expiring seat holds");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(
                    stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ExpireHoldsAsync(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var now = DateTime.UtcNow;

        int expiredCount = await context.Holds
            .Where(h =>
                h.Status == "HELD" &&
                h.ExpiresAt <= now)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        h => h.Status,
                        "EXPIRED"),
                cancellationToken);

        if (expiredCount > 0)
        {
            _logger.LogInformation(
                "Expired {Count} seat holds",
                expiredCount);
        }
    }
}
