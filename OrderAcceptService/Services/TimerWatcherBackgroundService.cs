using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderAcceptService.Services
{
    /// <summary>
    /// Worker chạy nền, mỗi 30 giây quét các OrderTimer hết hạn (Pending & Timeout <= now).
    /// Khi tìm thấy -> publish OrderAutoTimeoutExpiredEvent để OrderTimeoutConsumer xử lý.
    /// </summary>
    public class TimerWatcherBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBus _bus;
        private readonly ILogger<TimerWatcherBackgroundService> _logger;

        public TimerWatcherBackgroundService(
            IServiceScopeFactory scopeFactory,
            IBus bus,
            ILogger<TimerWatcherBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _bus = bus;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TimerWatcherBackgroundService đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanExpiredTimersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi quét OrderTimer hết hạn.");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ScanExpiredTimersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var timerRepo = scope.ServiceProvider.GetRequiredService<IOrderTimerRepositoryAsync>();

            var expired = await timerRepo.GetExpiredPendingAsync(DateTime.UtcNow);
            if (expired.Count == 0)
                return;

            _logger.LogInformation("Tìm thấy {Count} OrderTimer hết hạn cần xử lý.", expired.Count);

            foreach (var timer in expired)
            {
                stoppingToken.ThrowIfCancellationRequested();

                var targetAction = timer.Status == TargetStatus.Completed ? "Complete" : "Reject";

                await _bus.Publish(new OrderAutoTimeoutExpiredEvent(
                    Guid.NewGuid(),
                    timer.OrderId,
                    targetAction,
                    DateTime.UtcNow));

                timer.TimerStatus = TimerStatus.Processed;
                await timerRepo.UpdateAsync(timer);

                _logger.LogInformation("Đã publish OrderAutoTimeoutExpiredEvent OrderId={OrderId}, TargetAction={Action}.",
                    timer.OrderId, targetAction);
            }
        }
    }
}
