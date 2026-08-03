using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event thông báo Validate THÀNH CÔNG -> Saga gửi AcceptOrderCommand
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Items"></param>
    /// <param name="Timestamp"></param>
    public record OrderValidatedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemDto> Items,
        DateTime Timestamp
    ):IOrderEvent;
}
