using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command từ Saga gửi xuống OrderSubmitService để cancel đơn hàng
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Reason"></param>
    /// <param name="Timestamp"></param>
    public record CancelOrderCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string Reason,
        DateTime Timestamp
    ) : IOrderEvent;
}