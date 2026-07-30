using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Response khi Accept THÀNH CÔNG -> Đẩy SignalR về UI
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Noti"></param>
    /// <param name="Timestamp"></param>
    public record OrderAcceptSuccessResponse(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        NotificationPayLoad Noti,
        DateTime Timestamp
    ):IOrderEvent;
}