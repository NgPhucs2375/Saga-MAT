using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command từ Saga gửi xuống OrderSubmitService để accept đơn hàng
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
    ) : IOrderEvent;
}