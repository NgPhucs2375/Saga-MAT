using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command Saga gửi tới OrderSubmitService để validate sản phẩm & tồn kho
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Items"></param>
    /// <param name="Timestamp"></param>
    public record ValidateOrderCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        List<OrderItemDto> Items,
        DateTime Timestamp
    ):IOrderEvent;
}
