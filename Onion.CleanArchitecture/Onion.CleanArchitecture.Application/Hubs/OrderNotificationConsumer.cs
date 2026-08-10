using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Domain.Events;

namespace Onion.CleanArchitecture.Application.Hubs
{
    /// <summary>
    /// Consumer hợp nhất xử lý đẩy Notification về UI cho các luồng sự kiện của Order
    /// </summary>
    public class OrderNotificationConsumer : 
        IConsumer<OrderAcceptFailedResponse>,
        IConsumer<OrderAcceptSuccessResponse>,
        IConsumer<OrderCompleteFailedResponse>,
        IConsumer<OrderCompleteSuccessResponse>,
        IConsumer<OrderSubmitFailedResponse>,
        IConsumer<OrderSubmitSuccessResponse>
    {
        private readonly INotificationDispatcher _dispatcher;
        private readonly ILogger<OrderNotificationConsumer> _logger;

        public OrderNotificationConsumer(
            INotificationDispatcher dispatcher, 
            ILogger<OrderNotificationConsumer> logger)
        {
            _dispatcher = dispatcher;
            _logger = logger;
        }

        // 1. Xử lý OrderAcceptFailedResponse[cite: 3]
        public async Task Consume(ConsumeContext<OrderAcceptFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogWarning("Nhận OrderAcceptFailedResponse OrderId={OrderId}: {Error}", message.OrderId, message.ErrorReason);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }

        // 2. Xử lý OrderAcceptSuccessResponse[cite: 4]
        public async Task Consume(ConsumeContext<OrderAcceptSuccessResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderAcceptSuccessResponse OrderId={OrderId}", message.OrderId);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }

        // 3. Xử lý OrderCompleteFailedResponse[cite: 5]
        public async Task Consume(ConsumeContext<OrderCompleteFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogWarning("Nhận OrderCompleteFailedResponse OrderId={OrderId}: {Error}", message.OrderId, message.ErrorReason);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }

        // 4. Xử lý OrderCompleteSuccessResponse[cite: 6]
        public async Task Consume(ConsumeContext<OrderCompleteSuccessResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderCompleteSuccessResponse OrderId={OrderId}", message.OrderId);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }

        // 5. Xử lý OrderSubmitFailedResponse[cite: 7]
        public async Task Consume(ConsumeContext<OrderSubmitFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogWarning("Nhận OrderSubmitFailedResponse OrderId={OrderId}: {Error}", message.OrderId, message.ErrorMessage);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }

        // 6. Xử lý OrderSubmitSuccessResponse[cite: 8]
        public async Task Consume(ConsumeContext<OrderSubmitSuccessResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderSubmitSuccessResponse OrderId={OrderId}", message.OrderId);
            await _dispatcher.PushAsync(message.Noti, message.OrderId);
        }
    }
}