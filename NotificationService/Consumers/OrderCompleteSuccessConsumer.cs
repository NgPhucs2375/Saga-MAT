using MassTransit;
using NotificationService.Services;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Consumers
{
    /// <summary>
    /// Consumer OrderCompleteSuccessResponse -> đẩy Notification về UI
    /// </summary>
    public class OrderCompleteSuccessConsumer : IConsumer<OrderCompleteSuccessResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderCompleteSuccessConsumer> _logger;

        public OrderCompleteSuccessConsumer(INotificationDispatcher dispatcher, ILogger<OrderCompleteSuccessConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCompleteSuccessResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderCompleteSuccessResponse OrderId={OrderId}", message.OrderId);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}
