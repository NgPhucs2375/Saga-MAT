using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event khởi tạo đơn hàng từ UI -> Kích hoạt Saga (Submitted)
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Items"></param>
    /// <param name="TotalAmount"></param>
    /// <param name="Timestamp"></param>
    public record OrderCreatedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemDto> Items,
        decimal TotalAmount,
        DateTime Timestamp
    ):IOrderEvent;
}
