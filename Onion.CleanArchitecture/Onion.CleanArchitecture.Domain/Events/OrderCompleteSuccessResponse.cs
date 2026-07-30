using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Response khi Complete THÀNH CÔNG -> Kết thúc quy trình, đẩy SignalR
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Noti"></param>
    /// <param name="Timestamp"></param>
    public record OrderCompleteSuccessResponse(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        NotificationPayLoad Noti,
        DateTime Timestamp
    ):IOrderEvent;
}