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
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly ILogger<OrderSubmitConsumer> _logger;

        // === Constructor === //
        public OrderSubmitConsumer(
            IProductRepositoryAsync productRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            IOrderRepositoryAsync orderRepository,
            ILogger<OrderSubmitConsumer> logger)
        {
            _productRepository = productRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        // === Hàm Tiêu thụ ValidateOrderCommand === //
        public async Task Consume(ConsumeContext<ValidateOrderCommand> context)
        {
            // message = ValidateOrderCommand
            var message = context.Message;
            // Log cho ra string Nhận được command với ID
            _logger.LogInformation("Nhận ValidateOrderCommand OrderId={OrderId}", message.OrderId);

            // 1-2. Validate sản phẩm tồn tại (+ IsActive) và tồn kho khả dụng đủ
            var errors = new List<string>(); // biến chứ list lỗi
            var itemsToReserve = new List<OrderItemDto>();
            foreach (var item in message.Items) // Lặp qua all Items trong ValidateOrderCommand
            {
                // biến hứng dữ liệu của sản phẩm theo ID
                var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    errors.Add($"Sản phẩm {item.ProductId} không tồn tại hoặc đã bị vô hiệu hóa.");
                }
                else if (product.PhysicalQty - product.ReservedQty < item.Quantity)
                {
                    errors.Add($"Sản phẩm \"{product.Name}\" chỉ còn {product.PhysicalQty - product.ReservedQty} khả dụng trong kho, yêu cầu {item.Quantity}.");
                }
                else
                {
                    itemsToReserve.Add(item);
                }
            }

            if (errors.Count > 0)
            {
                // Fail -> OrderValidationFailedEvent để Saga chuyển Rejected (chưa giữ hàng)
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "ValidateOrderCommand", string.Join("; ", errors));
                await context.Publish(new OrderValidationFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId,
                    string.Join("; ", errors), DateTime.UtcNow));
                _logger.LogWarning("Validate thất bại OrderId={OrderId}: {Errors}", message.OrderId, string.Join("; ", errors));
                return;
            }

            // 3. Giữ hàng (Reserve) tại bước Submit — atomic + optimistic locking
            var reserved = new List<OrderItemDto>();
            foreach (var item in itemsToReserve)
            {
                var ok = await _productRepository.ReserveAsync(item.ProductId, item.Quantity);
                if (!ok)
                {
                    errors.Add($"Sản phẩm {item.ProductId} vừa được người khác chiếm, không đủ hàng khả dụng để giữ.");
                    break;
                }
                reserved.Add(item);
            }

            if (errors.Count > 0)
            {
                // Rollback phần đã giữ để không rò rỉ kho
                foreach (var item in reserved)
                {
                    await _productRepository.ReleaseAsync(item.ProductId, item.Quantity);
                }
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "ValidateOrderCommand", string.Join("; ", errors));
                await context.Publish(new OrderValidationFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId,
                    string.Join("; ", errors), DateTime.UtcNow));
                _logger.LogWarning("Giữ hàng thất bại, đã rollback. OrderId={OrderId}: {Errors}", message.OrderId, string.Join("; ", errors));
                return;
            }

            // 4. Đánh dấu đơn đang giữ hàng để compensate biết cần giải phóng
            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order != null)
            {
                order.IsReserved = true;
                await _orderRepository.UpdateAsync(order);
            }

            // Pass -> OrderValidatedEvent để Saga gửi AcceptOrderCommand
            await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "ValidateOrderCommand", "Validate thành công, đã giữ hàng (ReservedQty).");
            await context.Publish(new OrderValidatedEvent(
                NewId.NextGuid(), message.OrderId, message.CustomerId, message.Items, DateTime.UtcNow));
            _logger.LogInformation("Validate thành công OrderId={OrderId}, đã giữ hàng. Saga sẽ gửi AcceptOrderCommand", message.OrderId);
        }

        // === Hàm ghi OrderHistory === //
        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "OrderSubmitConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}
