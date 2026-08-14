
using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;

namespace OrderAcceptService
{
    /// <summary>
    /// Consumer bồi hoàn (compensate) bước Accept do Saga điều phối.
    /// Nhận CancelOrderCommand -> hủy đơn (Accepted -> Rejected) và vô hiệu hóa timer.
    /// Idempotent: chỉ xử lý khi đơn đang ở trạng thái Accepted.
    /// </summary>
    public class CancelOrderConsumer : IConsumer<CancelOrderCommand>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<CancelOrderConsumer> _logger;
        private readonly IMessageScheduler _messageScheduler;

        public CancelOrderConsumer(
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<CancelOrderConsumer> logger,
            IMessageScheduler messageScheduler)
        {
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
            _messageScheduler = messageScheduler;
        }

        public async Task Consume(ConsumeContext<CancelOrderCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận CancelOrderCommand OrderId={OrderId}, Reason={Reason}, bắt đầu bồi hoàn bước Accept.", message.OrderId, message.Reason);

            try
            {
                var order = await _orderRepository.GetByIdAsync(message.OrderId);
                if (order == null)
                {
                    _logger.LogError("Không tìm thấy OrderId={OrderId} để bồi hoàn.", message.OrderId);
                    return;
                }

                // Idempotency: Chỉ bồi hoàn nếu đơn đang ở trạng thái Accepted (bước Accept đã thành công).
                if (order.Status != OrderStatus.Accepted)
                {
                    _logger.LogWarning("OrderId={OrderId} không ở trạng thái 'Accepted' (trạng thái hiện tại: {Status}). Bỏ qua bồi hoàn.", message.OrderId, order.Status);
                    await context.Publish(new OrderCancelledEvent(
                        NewId.NextGuid(), 
                        message.OrderId, 
                        message.CustomerId, 
                        message.Reason, 
                        DateTime.UtcNow));
                    return;
                }

                // 1. Cập nhật trạng thái Order: Accepted -> Rejected
                order.Status = OrderStatus.Rejected;
                order.RejectedAt = DateTime.UtcNow;

                // 2. Vô hiệu hóa timer đang chờ
                var pendingTimer = await _orderTimerRepository.GetPendingByOrderIdAsync(message.OrderId);
                if (pendingTimer != null)
                {
                    pendingTimer.TimerStatus = TimerStatus.Cancelled;
                    if (Guid.TryParse(pendingTimer.JobId, out var tokenId))
                    {
                        await _messageScheduler.CancelScheduledPublish<OrderAutoTimeoutExpiredEvent>(tokenId);
                    }
                    await _orderTimerRepository.UpdateAsync(pendingTimer);
                }

                // 3. Lưu thay đổi Order
                await _orderRepository.UpdateAsync(order);

                // 4. Ghi lịch sử bồi hoàn thành công
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "Compensate",
                    $"Bồi hoàn bước Accept thành công: {message.Reason}");

                // 5. Báo Saga đã bồi hoàn xong (LIFO)
                await context.Publish(new OrderCancelledEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, message.Reason, DateTime.UtcNow));

                _logger.LogInformation("Bồi hoàn thành công OrderId={OrderId}.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi bồi hoàn OrderId={OrderId}.", message.OrderId);
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "Compensate",
                    $"Bồi hoàn thất bại do lỗi hệ thống: {ex.Message}");

                // Báo Saga bồi hoàn thất bại
                await context.Publish(new CancelOrderFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, $"Lỗi hệ thống khi bồi hoàn: {ex.Message}", DateTime.UtcNow));
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "CancelOrderConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}
