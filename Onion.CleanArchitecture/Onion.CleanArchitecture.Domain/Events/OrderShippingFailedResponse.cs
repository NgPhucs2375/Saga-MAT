using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    public record OrderShippingFailedResponse(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorReason,
        NotificationPayLoad Noti,
        DateTime Timestamp
    ):IOrderEvent;
}