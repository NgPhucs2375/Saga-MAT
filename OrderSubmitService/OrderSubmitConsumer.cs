using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderSubmitService
{
    /// <summary>
    /// Consumer xử lý ValidateOrderCommand từ Saga để validate sản phẩm & tồn kho
    /// </summary>
    public class OrderSubmitConsumer : IConsumer<ValidateOrderCommand>
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

        // === Hàm Tiêu thụ ValidateOrderCommand === //
        public async Task Consume(ConsumeContext<ValidateOrderCommand> context)
        {
            // message = ValidateOrderCommand
            var message = context.Message;
            // Log cho ra string Nhận được command với ID
            _logger.LogInformation("Nhận ValidateOrderCommand OrderId={OrderId}", message.OrderId);

            // 1-2. Validate sản phẩm tồn tại (+ IsActive) và tồn kho đủ
            var errors = new List<string>(); // biến chứ list lỗi
            foreach (var item in message.Items) // Lặp qua all Items trong ValidateOrderCommand
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

            if (!isSuccess)
            {
                // Fail -> OrderValidationFailedEvent để Saga chuyển Rejected
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "ValidateOrderCommand", string.Join("; ", errors));
                await context.Publish(new OrderValidationFailedEvent(
                    Guid.NewGuid(), message.OrderId, message.CustomerId,
                    string.Join("; ", errors), DateTime.UtcNow));
                _logger.LogWarning("Validate thất bại OrderId={OrderId}: {Errors}", message.OrderId, string.Join("; ", errors));
                return;
            }

            // Pass -> OrderValidatedEvent để Saga gửi AcceptOrderCommand
            await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "ValidateOrderCommand", "Validate thành công.");
            await context.Publish(new OrderValidatedEvent(
                Guid.NewGuid(), message.OrderId, message.CustomerId, message.Items, DateTime.UtcNow));
            _logger.LogInformation("Validate thành công OrderId={OrderId}, Saga sẽ gửi AcceptOrderCommand", message.OrderId);
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
