using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Events;

namespace NotificationService.Services
{
    public interface INotificationDispatcher
    {
        Task PushAsync(NotificationPayLoad noti, Guid orderId);
    }

    /// <summary>
    /// Nhận NotificationPayLoad từ các Response event -> đẩy SignalR về UI + lưu Notification (FR-17)
    /// </summary>
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotificationRepositoryAsync _notificationRepository;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(
            IHubContext<NotificationHub> hubContext,
            INotificationRepositoryAsync notificationRepository,
            ILogger<NotificationDispatcher> logger)
        {
            _hubContext = hubContext;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task PushAsync(NotificationPayLoad noti, Guid orderId)
        {
            // 1. Đẩy real-time về group của TargetUserId (client gọi Subscribe(userId) khi kết nối)
            await _hubContext.Clients
                .Group(noti.TargetUserId.ToString())
                .SendAsync("ReceiveNotification", noti);

            // 2. Lưu lịch sử Notification vào DB
            try
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    NotifyId = Guid.NewGuid(),
                    OrderId = orderId,
                    TargetUserId = noti.TargetUserId,
                    Title = noti.Title,
                    Message = noti.Message,
                    Type = noti.NotificationType,
                    IsRead = false,
                    SendAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lưu Notification thất bại OrderId={OrderId}", orderId);
            }
        }
    }
}
