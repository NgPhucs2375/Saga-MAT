using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderSubmitService.Consumer
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

            // 1. Validate sản phẩm vẫn tồn tại (+ IsActive).
            // Lưu ý: hàng đã được GIỮ (reserve) ngay trong CreateOrderCommand tại thời điểm tạo đơn
            // (atomic + optimistic), nên KHÔNG kiểm tra lại tồn kho ở đây để tránh double-count ReservedQty.
            var errors = new List<string>(); // biến chứ list lỗi
            foreach (var item in message.Items) // Lặp qua all Items trong ValidateOrderCommand
            {
                // biến hứng dữ liệu của sản phẩm theo ID
                var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                if (product == null || !product.IsActive)
                {
                    errors.Add($"Sản phẩm {item.ProductId} không tồn tại hoặc đã bị vô hiệu hóa.");
                }
            }

            if (errors.Count > 0)
            {
                // Fail -> mở khóa hàng đã giữ khi tạo đơn + cập nhật đơn sang Rejected cho khớp,
                // rồi publish OrderValidationFailedEvent để Saga chuyển Rejected.
                var orderFail = await _orderRepository.GetByIdAsync(message.OrderId);
                if (orderFail != null && orderFail.IsReserved)
                {
                    foreach (var item in message.Items)
                    {
                        await _productRepository.ReleaseAsync(item.ProductId, item.Quantity);
                    }
                    orderFail.IsReserved = false;
                    orderFail.Status = OrderStatus.Rejected;
                    orderFail.RejectedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(orderFail);
                }

                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "ValidateOrderCommand", string.Join("; ", errors));
                await context.Publish(new OrderValidationFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId,
                    string.Join("; ", errors), DateTime.UtcNow));
                _logger.LogWarning("Validate thất bại OrderId={OrderId}: {Errors}", message.OrderId, string.Join("; ", errors));
                return;
            }

            // Đảm bảo cờ giữ hàng được bật (đơn đã được reserve từ lúc tạo)
            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order != null && !order.IsReserved)
            {
                order.IsReserved = true;
                await _orderRepository.UpdateAsync(order);
            }

            // Pass -> OrderValidatedEvent để Saga gửi AcceptOrderCommand
            await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "ValidateOrderCommand", "Validate thành công (hàng đã giữ từ khi tạo đơn).");
            await context.Publish(new OrderValidatedEvent(
                NewId.NextGuid(), message.OrderId, message.CustomerId, message.Items, DateTime.UtcNow));
            _logger.LogInformation("Validate thành công OrderId={OrderId}, hàng đã giữ từ khi tạo đơn. Saga sẽ gửi AcceptOrderCommand", message.OrderId);
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
