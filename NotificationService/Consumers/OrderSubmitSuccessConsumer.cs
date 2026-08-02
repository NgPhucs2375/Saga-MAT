using MassTransit;
using NotificationService.Services;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Consumer OrderSubmitSuccessResponse -> đẩy Notification về UI
    /// </summary>
    public class OrderSubmitSuccessConsumer : IConsumer<OrderSubmitSuccessResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderSubmitSuccessConsumer> _logger;

        public OrderSubmitSuccessConsumer(INotificationDispatcher dispatcher, ILogger<OrderSubmitSuccessConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderSubmitSuccessResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderSubmitSuccessResponse OrderId={OrderId}", message.OrderId);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}
