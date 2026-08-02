using MassTransit;
using NotificationService.Services;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Consumer OrderAcceptSuccessResponse -> đẩy Notification về UI
    /// </summary>
    public class OrderAcceptSuccessConsumer : IConsumer<OrderAcceptSuccessResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderAcceptSuccessConsumer> _logger;

        public OrderAcceptSuccessConsumer(INotificationDispatcher dispatcher, ILogger<OrderAcceptSuccessConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderAcceptSuccessResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderAcceptSuccessResponse OrderId={OrderId}", message.OrderId);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}
