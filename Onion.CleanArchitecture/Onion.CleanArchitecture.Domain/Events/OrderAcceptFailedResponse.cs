using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Response khi Accept THẤT BẠI
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorReason"></param>
    /// <param name="Noti"></param>
    /// <param name="Timestamp"></param>
    public record OrderAcceptFailedResponse(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorReason,
        NotificationPayLoad Noti,
        DateTime Timestamp
    ):IOrderEvent;
}