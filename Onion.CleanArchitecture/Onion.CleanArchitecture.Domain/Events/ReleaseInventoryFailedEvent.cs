using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event OrderCompleteService báo Saga bồi hoàn tồn kho THẤT BẠI.
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorReason"></param>
    /// <param name="Timestamp"></param>
    public record ReleaseInventoryFailedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorReason,
        DateTime Timestamp
    ):IOrderEvent;
}
