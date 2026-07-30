using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event phát ra khi người dùng bấm đặt hàng (UI / API -> Broker)
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Items"></param>
    /// <param name="TotalAmount"></param>
    /// <param name="Timestamp"></param>
    public record OrderSubmittedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemDto> Items,
        decimal TotalAmount,
        DateTime Timestamp
    ) : IOrderEvent;


}