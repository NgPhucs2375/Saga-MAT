using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;

namespace ApproveOrderService.Consumers
{
    /// <summary>
    /// Consumer xử lý ApproveOrderCommand từ Saga (ApproveRequestedEvent -> SendApproveCommandActivity).
    /// Chuyển đơn PendingApproval -> Accepted, vô hiệu hóa timer,
    /// rồi publish OrderAcceptedEvent để Saga tiếp tục Completing.
    /// </summary>
    public class ApproveOrderConsumer : IConsumer<ApproveOrderCommand>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<ApproveOrderConsumer> _logger;
        private readonly IMessageScheduler _messageScheduler;

        public ApproveOrderConsumer(
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<ApproveOrderConsumer> logger,
            IMessageScheduler messageScheduler)
        {
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger = logger;
            _messageScheduler = messageScheduler;
        }

        public async Task Consume(ConsumeContext<ApproveOrderCommand> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận ApproveOrderCommand OrderId={OrderId}", message.OrderId);

            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogError("OrderId={OrderId} không tồn tại trong DB.", message.OrderId);
                return;
            }

            // Idempotency: Chỉ duyệt đơn đang chờ duyệt (retry/duplicate -> bỏ qua)
            if (order.Status != OrderStatus.PendingApproval)
            {
                _logger.LogWarning("OrderId={OrderId} không ở trạng thái PendingApproval (hiện tại: {Status}). Bỏ qua duyệt.", message.OrderId, order.Status);
                return;
            }

            try
            {
                order.Status = OrderStatus.Accepted;
                await _orderRepository.UpdateAsync(order);

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

                await RecordHistoryAsync(message.OrderId, HistoryStatus.Success, "ApproveOrderCommand", "Người duyệt đã đồng ý, đơn chuyển sang Accepted.");

                // Báo Saga -> Completing -> CompleteOrderCommand
                await context.Publish(new OrderAcceptedEvent(
                    NewId.NextGuid(), message.OrderId, message.CustomerId, DateTime.UtcNow));

                _logger.LogInformation("Duyệt thành công OrderId={OrderId}. Saga sẽ gửi CompleteOrderCommand.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi duyệt OrderId={OrderId}", message.OrderId);
                await RecordHistoryAsync(message.OrderId, HistoryStatus.Failed, "ApproveOrderCommand", $"Lỗi hệ thống khi duyệt: {ex.Message}");
            }
        }

        private async Task RecordHistoryAsync(Guid orderId, HistoryStatus status, string eventType, string message)
        {
            await _orderHistoryRepository.AddAsync(new OrderHistory
            {
                HistoryId = NewId.NextGuid(),
                OrderId = orderId,
                ConsumerName = "ApproveOrderConsumer",
                Status = status,
                EventType = eventType,
                Message = message
            });
        }
    }
}