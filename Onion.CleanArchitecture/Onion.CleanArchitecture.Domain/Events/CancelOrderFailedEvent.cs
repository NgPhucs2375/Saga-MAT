using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event OrderAcceptService báo Saga bồi hoàn (hủy đơn) THẤT BẠI.
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorReason"></param>
    /// <param name="Timestamp"></param>
    public record CancelOrderFailedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorReason,
        DateTime Timestamp
    ):IOrderEvent;
}
