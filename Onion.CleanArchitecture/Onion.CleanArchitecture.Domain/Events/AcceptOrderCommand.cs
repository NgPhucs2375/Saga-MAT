using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command Saga gửi tới OrderAcceptService để duyệt đơn & cài timer
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Timestamp"></param>
    public record AcceptOrderCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ):IOrderEvent;
}
