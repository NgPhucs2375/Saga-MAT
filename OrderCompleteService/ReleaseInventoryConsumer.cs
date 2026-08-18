using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;

namespace OrderCompleteService
{
    /// <summary>
    /// Consumer bồi hoàn (compensate) cho bước Validate/Complete do Saga điều phối.
    /// Nhận ReleaseInventoryCommand -> cộng lại tồn kho đã giữ chỗ và đưa đơn về Accepted
    /// để CancelOrderConsumer (OrderAcceptService) tiếp tục bồi hoàn (LIFO).
    /// Idempotent: chỉ bồi hoàn khi đơn còn cờ IsReserved = true (kho chưa được mở khóa).
    /// </summary>
    public class ReleaseInventoryConsumer : IConsumer<ReleaseInventoryCommand>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<ReleaseInventoryConsumer> _logger;

        public ReleaseInventoryConsumer(
            IOrderRepositoryAsync orderRepository,
            IProductRepositoryAsync productRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<ReleaseInventoryConsumer> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ReleaseInventoryCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận ReleaseInventoryCommand OrderId={OrderId}, bắt đầu bồi hoàn tồn kho.", message.OrderId);

            try
            {
                var order = await _orderRepository.GetByIdAsync(message.OrderId);
                if (order == null)
                {
                    _logger.LogError("Không tìm thấy OrderId={OrderId} để bồi hoàn.", message.OrderId);
                    await context.Publish(new ReleaseInventoryFailedEvent(
                        NewId.NextGuid(), message.OrderId, message.CustomerId, "Không tìm thấy đơn hàng để bồi hoàn tồn kho.", DateTime.UtcNow));
                    return;
                }

                // Idempotency: chỉ bồi hoàn nếu đơn còn giữ chỗ (IsReserved=true). 
                // Nếu đã mở khóa rồi (retry/duplicate) thì bỏ qua, không cộng kho lần 2.
                if (!order.IsReserved)
                {
                    _logger.LogWarning("OrderId={OrderId} không còn giữ chỗ tồn kho (IsReserved=false). Bỏ qua bồi hoàn.", message.OrderId);
                    await context.Publish(new InventoryReleasedEvent(
                        NewId.NextGuid(), message.OrderId, message.CustomerId, DateTime.UtcNow));
                    return;
                }

                // 1. Giải phóng giữ chỗ: giảm ReservedQty cho từng sản phẩm đã được giữ.
                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ReservedQty -= item.Quantity;
                        // ApplicationDbContext NoTracking -> phải đánh dấu Modified để SaveChanges ghi lại.
                        _productRepository.MarkAsModified(product);
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy sản phẩm ProductId={ProductId} để hoàn lại tồn kho cho OrderId={OrderId}.", item.ProductId, message.OrderId);
                    }
                }

                // 2. Hết giữ chỗ (mở khóa) và đưa đơn về Accepted để CancelOrderConsumer bồi hoàn tiếp.
                order.IsReserved = false;
                order.Status = OrderStatus.Accepted;

                // 3. Lưu tất cả thay đổi trong một SaveChanges.
                await _orderRepository.UpdateAsync(order);

                // 4. Ghi lịch sử bồi hoàn thành công.
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "Compensate",
                    $"Bồi hoàn tồn kho thành công: {message.Reason}");

                // 5. Báo Saga bồi hoàn xong (LIFO -> tiếp tục CancelOrder).
                await context.Publish(new InventoryReleasedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, DateTime.UtcNow));

                _logger.LogInformation("Bồi hoàn tồn kho thành công OrderId={OrderId}.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi bồi hoàn tồn kho OrderId={OrderId}.", message.OrderId);
                // await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "Compensate",
                //     $"Bồi hoàn tồn kho thất bại do lỗi hệ thống: {ex.Message}");

                // await context.Publish(new ReleaseInventoryFailedEvent(
                //     NewId.NextGuid(), message.OrderId, message.CustomerId, $"Lỗi hệ thống khi bồi hoàn tồn kho: {ex.Message}", DateTime.UtcNow));

                throw;
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "ReleaseInventoryConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}