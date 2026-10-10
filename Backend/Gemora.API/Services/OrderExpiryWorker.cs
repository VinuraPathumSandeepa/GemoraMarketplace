using Gemora.Application.Interfaces;

namespace Gemora.API.Services;

// Runs without either party keeping a browser open. Restarts catch up on overdue orders.
public sealed class OrderExpiryWorker(IServiceScopeFactory scopes, ILogger<OrderExpiryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IOrderService>().ExpireUnpaidAsync();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to expire unpaid orders; will retry on the next sweep.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
