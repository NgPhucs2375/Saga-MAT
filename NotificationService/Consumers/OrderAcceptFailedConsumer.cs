using MassTransit;
using NotificationService.Services;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Consumer OrderAcceptFailedResponse -> đẩy Notification về UI
    /// </summary>
    public class OrderAcceptFailedConsumer : IConsumer<OrderAcceptFailedResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderAcceptFailedConsumer> _logger;

        public OrderAcceptFailedConsumer(INotificationDispatcher dispatcher, ILogger<OrderAcceptFailedConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderAcceptFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogWarning("Nhận OrderAcceptFailedResponse OrderId={OrderId}: {Error}", message.OrderId, message.ErrorReason);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}
