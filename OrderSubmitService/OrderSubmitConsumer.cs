using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderSubmitService
{
    /// <summary>
    /// Consumer xử lý OrderSubmittedEvent từ OrderSubmitService
    /// </summary>
    public class OrderSubmitConsumer : IConsumer<OrderSubmittedEvent>
    {
        // === Tiêm các Repository cần thiết === //
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<OrderSubmitConsumer> _logger;

        // === Constructor === //
        public OrderSubmitConsumer(
            IProductRepositoryAsync productRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<OrderSubmitConsumer> logger)
        {
            _productRepository = productRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
        }

        // === Hàm Tiêu thụ OrderSubmittedEvent === //
        public async Task Consume(ConsumeContext<OrderSubmittedEvent> context)
        {
            // message = OrderSubmittedEvent
            var message = context.Message;
            // Log cho ra string Nhận được event với ID 
            _logger.LogInformation("Nhận OrderSubmittedEvent OrderId={OrderId}", message.OrderId);

            // 1-2. Validate sản phẩm tồn tại (+ IsActive) và tồn kho đủ
            var errors = new List<string>(); // biến chứ list lỗi 
            foreach (var item in message.Items) // Lặp qua all Items trong OrderSubmittedEvent
            {
                // biến hứng dữ liệu của sản phẩm theo ID
                var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    errors.Add($"Sản phẩm {item.ProductId} không tồn tại hoặc đã bị vô hiệu hóa.");
                }
                else if (product.SLTKho < item.Quantity)
                {
                    errors.Add($"Sản phẩm \"{product.Name}\" chỉ còn {product.SLTKho} trong kho, yêu cầu {item.Quantity}.");
                }
            }

            // Biến check xem có lỗi hay không, nếu errors.Count > 0 thì là có lỗi
            var isSuccess = errors.Count == 0;
            // === Noti === //
            var noti = new NotificationPayLoad(
                message.CustomerId,
                "Đơn hàng",
                isSuccess ? "Đơn hàng đã được xác nhận." : "Đơn hàng đã bị từ chối.",
                isSuccess ? "Success" : "Error",
                DateTime.UtcNow);

            if (!isSuccess)
            {
                // 4. Fail
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "OrderSubmittedEvent", string.Join("; ", errors));
                await context.Publish(new OrderSubmitFailedResponse(
                    Guid.NewGuid(), message.OrderId, message.CustomerId,
                    string.Join("; ", errors), noti, DateTime.UtcNow));
                _logger.LogWarning("Submit thất bại OrderId={OrderId}: {Errors}", message.OrderId, string.Join("; ", errors));
                return;
            }

            // 3. Pass -> Success Response + kích hoạt Accept
            await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "OrderSubmittedEvent", "Validate thành công.");
            await context.Publish(new OrderSubmitSuccessResponse(
                Guid.NewGuid(), message.OrderId, message.CustomerId, noti, DateTime.UtcNow));
            await context.Publish(new ProcessOrderAcceptCommand(
                Guid.NewGuid(), message.OrderId, message.CustomerId, DateTime.UtcNow));
            _logger.LogInformation("Submit thành công OrderId={OrderId}, chuyển tiếp OrderAcceptService", message.OrderId);
        }

        // === Hàm ghi OrderHistory === //
        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = Guid.NewGuid(),
                OrderId = orderId,
                ConsumerName = "OrderSubmitConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}