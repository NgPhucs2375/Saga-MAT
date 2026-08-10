using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command từ người duyệt (UI/API) -> OrderAcceptService duyệt đơn đang chờ duyệt.
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Timestamp"></param>
    public record ApproveOrderCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ) : IOrderEvent;
}