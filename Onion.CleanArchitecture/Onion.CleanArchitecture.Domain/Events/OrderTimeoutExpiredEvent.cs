using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// OrderSubmitService thông báo đơn hàng timeout -> Saga compensate
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Timestamp"></param>
     public record OrderTimeoutExpiredEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ) : IOrderEvent;
}