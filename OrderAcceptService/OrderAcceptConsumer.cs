using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderAcceptService
{
    /// <summary>
    /// Consumer xử lý AcceptOrderCommand từ Saga để duyệt đơn & cài timer
    /// </summary>
    public class OrderAcceptConsumer : IConsumer<AcceptOrderCommand>
    {
        // === Tiêm các Repository cần thiết === //
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<OrderAcceptConsumer> _logger;
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IConfiguration _configuration;

        // === Constructor === //
        public OrderAcceptConsumer(
            IProductRepositoryAsync productRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<OrderAcceptConsumer> logger,
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IConfiguration configuration
            )
        {
            _productRepository = productRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _configuration = configuration;
        }

        // === Hàm Tiêu thụ AcceptOrderCommand === //
        public async Task Consume(ConsumeContext<AcceptOrderCommand> context)
        {
            var message = context.Message;
            var order = await _orderRepository.GetByIdAsync(message.OrderId);

            try
            {
                if (order == null)
                {
                    _logger.LogError("OrderId={OrderId} không tồn tại trong DB.", message.OrderId);
                    return;
                }
                // Check chống trùng (idempotency) - nếu đã xử lý rồi (retry/duplicate)
                if (order.Status != OrderStatus.Submitted)
                {
                    _logger.LogWarning("OrderId={OrderId} không ở trạng thái Submitted (trạng thái hiện tại: {Status}). Bỏ qua xử lý.", message.OrderId, order.Status);
                    return;
                }
                _logger.LogInformation("Nhận AcceptOrderCommand OrderId={OrderId}", message.OrderId);

                // === Re-validate sp tồn tại (+IsActive) và kho đủ === //
                var errors = new List<string>();
                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product == null || !product.IsActive)
                    {
                        errors.Add($"Sản phẩm {item.ProductId} không tồn tại hoặc đã bị vô hiệu hóa.");
                    }
                    else if (product.PhysicalQty - product.ReservedQty < item.Quantity)
                    {
                        errors.Add($"Sản phẩm \"{product.Name}\" chỉ còn {product.PhysicalQty - product.ReservedQty} khả dụng trong kho, yêu cầu {item.Quantity}.");
                    }
                }

                var isSuccess = errors.Count == 0;

                if (!isSuccess)
                {
                    // === XỬ LÝ THẤT BẠI (VALIDATION FAILED) ===
                    var errorReason = string.Join("; ", errors);
                    order.Status = OrderStatus.Rejected;
                    order.RejectedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(order);

                    await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "AcceptOrderCommand", errorReason);

                    await context.Publish(new OrderAcceptFailedEvent(
                        NewId.NextGuid(), message.OrderId, message.CustomerId, errorReason, DateTime.UtcNow));

                    _logger.LogWarning("Accept thất bại OrderId={OrderId}: {Errors}", message.OrderId, errorReason);
                    return;
                }

                // === XỬ LÝ THÀNH CÔNG ===
                // 1. Cập nhật trạng thái Order
                order.Status = OrderStatus.Accepted;
                // UpdateAsync sẽ tự động set UpdatedAt
                await _orderRepository.UpdateAsync(order);

                // 2. Tạo và lưu OrderTimer
                var orderTimer = new OrderTimer
                {
                    TimerId = NewId.NextGuid(),
                    OrderId = message.OrderId,
                    Timeout = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("OrderAcceptTimeoutMinutes")),
                    Status = TargetStatus.Completed,
                    TimerStatus = TimerStatus.Pending,
                };
                await _orderTimerRepository.AddAsync(orderTimer);

                // 3. Ghi lịch sử và publish event cho Saga
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "AcceptOrderCommand", "Re-Validate thành công, đơn hàng đã được chấp nhận.");

                await context.Publish(new OrderAcceptedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, _configuration.GetValue<int>("OrderAcceptTimeoutMinutes"), DateTime.UtcNow));

                _logger.LogInformation("Accept thành công OrderId={OrderId}, Saga sẽ gửi CompleteOrderCommand", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi xử lý OrderAcceptConsumer cho OrderId={OrderId}", message.OrderId);

                // Bồi hoàn nếu có thể: chuyển đơn hàng sang trạng thái Rejected để không retry vô hạn
                if (order != null && order.Status == OrderStatus.Submitted)
                {
                    var errorReason = $"Lỗi hệ thống: {ex.Message}";
                    order.Status = OrderStatus.Rejected;
                    order.RejectedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(order);

                    await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "AcceptOrderCommand", errorReason);

                    await context.Publish(new OrderAcceptFailedEvent(
                        NewId.NextGuid(), message.OrderId, message.CustomerId, errorReason, DateTime.UtcNow));
                }
                // Không throw lại exception để message được coi là đã xử lý (consumed) và không bị retry.
            }
        }

        // === Hàm ghi OrderHistory === //
        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "OrderAcceptConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}
