using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event thông báo Complete THÀNH CÔNG -> Saga chuyển Completed
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Timestamp"></param>
    public record OrderCompletedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ):IOrderEvent;
}
