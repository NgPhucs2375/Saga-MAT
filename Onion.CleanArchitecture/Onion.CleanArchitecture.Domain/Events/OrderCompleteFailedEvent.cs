using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event thông báo Complete THẤT BẠI -> Saga gửi CancelOrderCommand (compensate)
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorReason"></param>
    /// <param name="Timestamp"></param>
    public record OrderCompleteFailedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorReason,
        DateTime Timestamp
    ):IOrderEvent;
}
