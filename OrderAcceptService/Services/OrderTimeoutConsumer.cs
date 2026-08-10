using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;
using System.Threading.Tasks;

namespace OrderAcceptService
{
    /// <summary>
    /// Consumer xử lý khi một đơn hàng hết hạn chờ duyệt.
    /// Được kích hoạt bởi OrderAutoTimeoutExpiredEvent (TargetAction=Reject).
    /// Không tự sửa DB/bồi hoàn: chỉ vô hiệu hóa timer rồi publish OrderAcceptFailedEvent
    /// để Saga chạy LIFO compensation (ReleaseInventory -> Cancel -> Rejected) một nguồn sự thật duy nhất.
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

                // Idempotency check: Chỉ xử lý nếu đơn vẫn đang ở trạng thái chờ duyệt.
                if (order.Status != OrderStatus.PendingApproval)
                {
                    _logger.LogWarning("OrderId={OrderId} không ở trạng thái 'PendingApproval' (hiện tại: {Status}). Hủy timer và bỏ qua.", message.OrderId, order.Status);
                    timer.TimerStatus = TimerStatus.Cancelled;
                    await _orderTimerRepository.UpdateAsync(timer);
                    return;
                }

                if (string.Equals(message.TargetAction, nameof(TargetStatus.Rejected), StringComparison.OrdinalIgnoreCase))
                {
                    // === TIME-OUT REJECT BRANCH ===
                    _logger.LogInformation("OrderId={OrderId} đã hết thời gian chờ duyệt. Thực hiện từ chối tự động.", message.OrderId);

                    // 1. Đánh dấu timer đã xử lý
                    timer.TimerStatus = TimerStatus.Processed;
                    await _orderTimerRepository.UpdateAsync(timer);

                    // 2. Ghi lịch sử
                    await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "TimeoutReject", "Đơn hàng bị từ chối tự động do quá thời gian chờ duyệt.");

                    // 3. Báo Saga -> bồng hoàn LIFO (Release -> Cancel) -> Rejected + thông báo UI
                    await context.Publish(new OrderAcceptFailedEvent(
                        NewId.NextGuid(), message.OrderId, new Guid(order.CustomerId), "Timeout", DateTime.UtcNow));
                }
                else
                {
                    _logger.LogError("TargetAction không xác định '{Action}' cho OrderId={OrderId}", message.TargetAction, message.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi xử lý timeout cho OrderId={OrderId}", message.OrderId);
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "TimeoutProcessing", $"Xử lý timeout thất bại: {ex.Message}");
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "OrderTimeoutConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}