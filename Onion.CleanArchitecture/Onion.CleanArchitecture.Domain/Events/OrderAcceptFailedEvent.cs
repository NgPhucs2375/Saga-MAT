using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event thông báo Accept THẤT BẠI -> Saga chuyển Rejected
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorReason"></param>
    /// <param name="Timestamp"></param>
    public record OrderAcceptFailedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorReason,
        DateTime Timestamp
    ):IOrderEvent;
}
