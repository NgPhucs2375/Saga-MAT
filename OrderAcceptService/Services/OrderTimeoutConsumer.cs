using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrderAcceptService
{
    /// <summary>
    /// Consumer xử lý khi một đơn hàng hết hạn chờ.
    /// Được kích hoạt bởi OrderAutoTimeoutExpiredEvent.
    /// </summary>
    public class OrderTimeoutConsumer : IConsumer<OrderAutoTimeoutExpiredEvent>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<OrderTimeoutConsumer> _logger;

        public OrderTimeoutConsumer(
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<OrderTimeoutConsumer> logger)
        {
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderAutoTimeoutExpiredEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderAutoTimeoutExpiredEvent cho OrderId={OrderId}, TargetAction={Action}", message.OrderId, message.TargetAction);

            try
            {
                var order = await _orderRepository.GetByIdAsync(message.OrderId);
                var timer = await _orderTimerRepository.GetPendingByOrderIdAsync(message.OrderId);

                if (order == null || timer == null)
                {
                    _logger.LogWarning("Không tìm thấy Order hoặc OrderTimer đang chờ cho OrderId={OrderId}. Bỏ qua xử lý timeout.", message.OrderId);
                    return;
                }

                // Idempotency check: Chỉ xử lý nếu đơn hàng vẫn đang ở trạng thái Accepted.
                if (order.Status != OrderStatus.Accepted)
                {
                    _logger.LogWarning("OrderId={OrderId} không ở trạng thái 'Accepted' (trạng thái hiện tại: {Status}). Hủy timer và bỏ qua.", message.OrderId, order.Status);
                    timer.TimerStatus = TimerStatus.Cancelled;
                    await _orderTimerRepository.UpdateAsync(timer);
                    return;
                }

                // Xử lý theo TargetAction từ event
                if (string.Equals(message.TargetAction, nameof(TargetStatus.Rejected), StringComparison.OrdinalIgnoreCase))
                {
                    // === REJECT BRANCH ===
                    _logger.LogInformation("OrderId={OrderId} đã hết hạn. Thực hiện Reject tự động.", message.OrderId);

                    // 1. Cập nhật trạng thái Order
                    order.Status = OrderStatus.Rejected;
                    order.RejectedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(order); // UpdatedAt được set tự động

                    // 2. Cập nhật trạng thái Timer -> Processed
                    timer.TimerStatus = TimerStatus.Processed;
                    await _orderTimerRepository.UpdateAsync(timer); // Cập nhật tường minh

                    // 3. Ghi lịch sử
                    await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "TimeoutReject", "Đơn hàng bị từ chối tự động do hết hạn xử lý.");

                    // 4. Gửi thông báo cho UI
                    var noti = new NotificationPayLoad(
                        new Guid(order.CustomerId), "Đơn hàng bị từ chối", "Đơn hàng của bạn đã bị từ chối tự động do quá thời gian xử lý.", "Warning", DateTime.UtcNow);
                    
                    await context.Publish(new OrderAcceptFailedResponse(
                        Guid.NewGuid(), message.OrderId, new Guid(order.CustomerId), "Timeout", noti, DateTime.UtcNow));
                }
                else if (string.Equals(message.TargetAction, nameof(TargetStatus.Completed), StringComparison.OrdinalIgnoreCase))
                {
                    // === COMPLETE BRANCH ===
                    _logger.LogInformation("OrderId={OrderId} đã hết hạn. Kích hoạt Complete tự động.", message.OrderId);

                    timer.TimerStatus = TimerStatus.Processed;
                    await _orderTimerRepository.UpdateAsync(timer);

                    await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "TimeoutCompleteTrigger", "Kích hoạt hoàn tất đơn hàng tự động do hết hạn.");

                    // Kích hoạt OrderCompleteConsumer để xử lý logic trừ kho
                    await context.Publish(new OrderCompleteEvent(
                        Guid.NewGuid(), order.OrderId, new Guid(order.CustomerId),
                        order.OrderItems.Select(oi => new OrderItemDto(oi.ProductId, oi.Quantity, oi.UnitPrice)).ToList(),
                        DateTime.UtcNow));
                }
                else
                {
                    _logger.LogError("TargetAction không xác định '{Action}' cho OrderId={OrderId}", message.TargetAction, message.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi xử lý timeout cho OrderId={OrderId}", message.OrderId);
                // Ghi history failed khi có exception
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "TimeoutProcessing", $"Xử lý timeout thất bại: {ex.Message}");
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = Guid.NewGuid(),
                OrderId = orderId,
                ConsumerName = "OrderTimeoutConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}
