using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command từ người duyệt (UI/API) -> OrderAcceptService từ chối đơn đang chờ duyệt.
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Reason"></param>
    /// <param name="Timestamp"></param>
    public record RejectOrderCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string Reason,
        DateTime Timestamp
    ) : IOrderEvent;
}