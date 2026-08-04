using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command Saga gửi tới OrderCompleteService để bồi hoàn (compensate) bước Complete:
    /// cộng lại tồn kho đã trừ.
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Items"></param>
    /// <param name="Reason"></param>
    /// <param name="Timestamp"></param>
    public record ReleaseInventoryCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemDto> Items,
        string Reason,
        DateTime Timestamp
    ):IOrderEvent;
}
