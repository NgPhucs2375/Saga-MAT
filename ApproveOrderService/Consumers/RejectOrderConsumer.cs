using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace ApproveOrderService.Consumers
{
    /// <summary>
    /// Consumer xử lý RejectOrderCommand từ Saga (RejectRequestedEvent -> SendRejectCommandActivity).
    /// Publish OrderAcceptFailedEvent để Saga chạy bồi hoàn LIFO
    /// (ReleaseInventory -> Cancel) và vô hiệu hóa timer chờ duyệt.
    /// </summary>
    public class RejectOrderConsumer : IConsumer<RejectOrderCommand>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<RejectOrderConsumer> _logger;
        private readonly IMessageScheduler _messageScheduler;

        public RejectOrderConsumer(
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<RejectOrderConsumer> logger,
            IMessageScheduler messageScheduler)
        {
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
            _messageScheduler = messageScheduler;
        }

        public async Task Consume(ConsumeContext<RejectOrderCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận RejectOrderCommand OrderId={OrderId}, Reason={Reason}", message.OrderId, message.Reason);

            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogError("OrderId={OrderId} không tồn tại trong DB.", message.OrderId);
                return;
            }

            // Idempotency: Chỉ từ chối đơn đang chờ duyệt (retry/duplicate -> bỏ qua)
            if (order.Status != OrderStatus.PendingApproval)
            {
                _logger.LogWarning("OrderId={OrderId} không ở trạng thái PendingApproval (hiện tại: {Status}). Bỏ qua từ chối.", message.OrderId, order.Status);
                return;
            }

            try
            {
                // Vô hiệu hóa timer chờ duyệt
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

                var reason = string.IsNullOrWhiteSpace(message.Reason) ? "Bị từ chối bởi người duyệt" : message.Reason;
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "RejectOrderCommand", reason);

                // Báo Saga -> bồi hoàn LIFO -> Rejected
                await context.Publish(new OrderAcceptFailedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, reason, DateTime.UtcNow));

                _logger.LogInformation("Từ chối thành công OrderId={OrderId}. Saga sẽ bồi hoàn.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi từ chối OrderId={OrderId}", message.OrderId);
                // await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "RejectOrderCommand", $"Lỗi hệ thống khi từ chối: {ex.Message}");

                throw;
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "RejectOrderConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}