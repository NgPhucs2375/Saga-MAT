using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;

namespace OrderAcceptService
{
    public class OrderCompleteFailedConsumer : IConsumer<OrderCompleteFailedResponse>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IOrderTimerRepositoryAsync _orderTimerRepository;
        private readonly IOrderHistoryRepositoryAsync _orderHistoryRepository;
        private readonly ILogger<OrderCompleteFailedConsumer> _logger;

        public OrderCompleteFailedConsumer(
            IOrderRepositoryAsync orderRepository,
            IOrderTimerRepositoryAsync orderTimerRepository,
            IOrderHistoryRepositoryAsync orderHistoryRepository,
            ILogger<OrderCompleteFailedConsumer> logger
            )
        {
            _orderRepository = orderRepository;
            _orderTimerRepository = orderTimerRepository;
            _orderHistoryRepository = orderHistoryRepository;
            _logger =logger;
        }

        public async Task Consume(ConsumeContext<OrderCompleteFailedResponse> context)
        {
            var message = context.Message;
            _logger.LogInformation("Nhận OrderCompleteFailedResponse cho OrderId={OrderId}, bắt đầu bồi hoàn.", message.OrderId);

            var order = await _orderRepository.GetByIdAsync(message.OrderId);
            if (order == null)
            {
                _logger.LogError("Không tìm thấy OrderId={OrderId} để bồi hoàn.", message.OrderId);
                return;
            }

            if (order.Status != OrderStatus.Accepted)
            {
                _logger.LogWarning("OrderId={OrderId} không ở trạng thái 'Accepted' (trạng thái hiện tại: {Status}). Bỏ qua bồi hoàn.", message.OrderId, order.Status);
                return;
            }

            try
            {
                // 1. Cập nhật trạng thái Order: Accepted -> Rejected
                order.Status = OrderStatus.Rejected;
                order.RejectedAt = DateTime.UtcNow;
                // UpdatedAt sẽ được tự động cập nhật bởi AuditableBaseEntity khi SaveChangesAsync được gọi.

                // 2. Vô hiệu hóa Timer
                var pendingTimer = await _orderTimerRepository.GetPendingByOrderIdAsync(message.OrderId);
                if (pendingTimer != null)
                {
                    pendingTimer.TimerStatus = TimerStatus.Cancelled;
                    // 3. Lưu thay đổi của Timer một cách tường minh
                    await _orderTimerRepository.UpdateAsync(pendingTimer);
                    _logger.LogInformation("Đã hủy và lưu OrderTimer cho OrderId={OrderId}.", message.OrderId);
                }

                // 4. Lưu thay đổi của Order
                await _orderRepository.UpdateAsync(order);

                // 5. Ghi lịch sử bồi hoàn thành công
                var historyMessage = $"Hoàn lại trạng thái do Complete thất bại: {message.ErrorReason}";
                await _orderHistoryRepository.AddAsync(new OrderHistory
                {
                    HistoryId = NewId.NextGuid(),
                    OrderId = message.OrderId,
                    ConsumerName = "OrderCompleteFailedConsumer",
                    Status = HistoryStatus.Success,
                    EventType = "Compensate",
                    Message = historyMessage
                });

                // 6. Publish response để UI cập nhật
                var noti = new NotificationPayLoad(
                    message.CustomerId,
                    "Đơn hàng bị từ chối",
                    $"Đơn hàng của bạn đã bị từ chối do có vấn đề ở khâu xử lý. Lý do: {message.ErrorReason}",
                    "Error",
                    DateTime.UtcNow);

                // Sử dụng OrderAcceptFailedResponse để UI có thể tái sử dụng logic hiển thị lỗi
                await context.Publish(new OrderAcceptFailedResponse(
                    NewId.NextGuid(),
                    message.OrderId,
                    message.CustomerId,
                    message.ErrorReason, // Lý do gốc từ OrderCompleteFailed
                    noti,
                    DateTime.UtcNow
                ));

                _logger.LogInformation("Bồi hoàn thành công cho OrderId={OrderId}.", message.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không mong muốn khi bồi hoàn cho OrderId={OrderId}.", message.OrderId);
                await _orderHistoryRepository.AddAsync(new OrderHistory
                {
                    HistoryId = NewId.NextGuid(),
                    OrderId = message.OrderId,
                    ConsumerName = "OrderCompleteFailedConsumer",
                    Status = HistoryStatus.Failed, // Bồi hoàn thất bại
                    EventType = "Compensate",
                    Message = $"Bồi hoàn thất bại do lỗi hệ thống: {ex.Message}"
                });
            }
        }
    }
}