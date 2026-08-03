using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command Saga gửi tới OrderCompleteService để hoàn tất đơn & trừ kho
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Items"></param>
    /// <param name="Timestamp"></param>
    public record CompleteOrderCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemDto> Items,
        DateTime Timestamp
    ):IOrderEvent;
}
