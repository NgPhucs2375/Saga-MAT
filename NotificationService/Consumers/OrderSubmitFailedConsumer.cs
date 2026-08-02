using MassTransit;
using NotificationService.Services;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Consumer OrderSubmitFailedResponse -> đẩy Notification về UI
    /// </summary>
    public class OrderSubmitFailedConsumer : IConsumer<OrderSubmitFailedResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderSubmitFailedConsumer> _logger;

        public OrderSubmitFailedConsumer(INotificationDispatcher dispatcher, ILogger<OrderSubmitFailedConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderSubmitFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogWarning("Nhận OrderSubmitFailedResponse OrderId={OrderId}: {Error}", message.OrderId, message.ErrorMessage);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}
