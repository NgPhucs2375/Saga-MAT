using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;
using System.Threading.Tasks;

namespace OrderCompleteService
{
    /// <summary>
    /// Consumer bồi hoàn cho OrderComplete khi bước Shipping (giả định) thất bại.
    /// Nhiệm vụ: Cộng lại tồn kho và chuyển trạng thái đơn hàng về Rejected.
    /// </summary>
    public class ShippingFailedConsumer : IConsumer<OrderShippingFailedResponse>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<ShippingFailedConsumer> _logger;

        public ShippingFailedConsumer(
            IOrderRepositoryAsync orderRepository,
            IProductRepositoryAsync productRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<ShippingFailedConsumer> logger)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderShippingFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderShippingFailedResponse cho OrderId={OrderId}, bắt đầu bồi hoàn tồn kho.", message.OrderId);

            try
            {
                var order = await _orderRepository.GetByIdAsync(message.OrderId);
                if (order == null)
                {
                    _logger.LogError("Không tìm thấy OrderId={OrderId} để bồi hoàn.", message.OrderId);
                    return;
                }

                // Idempotency: Chỉ bồi hoàn nếu đơn hàng đã ở trạng thái Completed.
                if (order.Status != OrderStatus.Completed)
                {
                    _logger.LogWarning("OrderId={OrderId} không ở trạng thái 'Completed' (trạng thái hiện tại: {Status}). Bỏ qua bồi hoàn.", message.OrderId, order.Status);
                    return;
                }

                // Cộng lại SLTKho cho từng sản phẩm.
                // EF Core's change tracker sẽ theo dõi các thay đổi trên product entities.
                foreach (var item in order.OrderItems)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product != null)
                    {
                        product.SLTKho += item.Quantity;
                    }
                    else
                    {
                        _logger.LogWarning("Không tìm thấy sản phẩm ProductId={ProductId} để hoàn lại tồn kho cho OrderId={OrderId}.", item.ProductId, message.OrderId);
                    }
                }

                // Revert trạng thái đơn hàng: Completed -> Rejected.
                order.Status = OrderStatus.Rejected;
                order.RejectedAt = DateTime.UtcNow;
                // UpdatedAt sẽ được tự động cập nhật bởi AuditableBaseEntity.

                // Lưu tất cả thay đổi (Order status và Product SLTKho) trong một transaction.
                await _orderRepository.UpdateAsync(order);

                // Ghi lịch sử bồi hoàn thành công.
                var historyMessage = $"Hoàn lại tồn kho và trạng thái do Shipping thất bại: {message.ErrorReason}";
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "Compensate", historyMessage);

                // Publish notification để UI biết đơn hàng đã bị hủy.
                var noti = new NotificationPayLoad(
                    message.CustomerId, "Đơn hàng đã bị hủy", $"Đơn hàng của bạn đã bị hủy do có lỗi ở khâu vận chuyển. Lý do: {message.ErrorReason}", "Error", DateTime.UtcNow);

                await context.Publish(new OrderCompleteFailedResponse(
                    Guid.NewGuid(), message.OrderId, message.CustomerId, $"Bồi hoàn do Shipping thất bại: {message.ErrorReason}", noti, DateTime.UtcNow));

                _logger.LogInformation("Bồi hoàn tồn kho và trạng thái thành công cho OrderId={OrderId}.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi bồi hoàn tồn kho cho OrderId={OrderId}.", message.OrderId);
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "Compensate", $"Bồi hoàn tồn kho thất bại do lỗi hệ thống: {ex.Message}");
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = Guid.NewGuid(),
                OrderId = orderId,
                ConsumerName = "ShippingFailedConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}