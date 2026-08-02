using MassTransit;
using NotificationService.Services;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Consumer OrderCompleteFailedResponse -> đẩy Notification về UI
    /// </summary>
    public class OrderCompleteFailedConsumer : IConsumer<OrderCompleteFailedResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderCompleteFailedConsumer> _logger;

        public OrderCompleteFailedConsumer(INotificationDispatcher dispatcher, ILogger<OrderCompleteFailedConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCompleteFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogWarning("Nhận OrderCompleteFailedResponse OrderId={OrderId}: {Error}", message.OrderId, message.ErrorReason);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}
