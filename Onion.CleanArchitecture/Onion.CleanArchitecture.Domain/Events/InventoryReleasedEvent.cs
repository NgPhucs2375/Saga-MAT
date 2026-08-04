using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event OrderCompleteService báo Saga đã bồi hoàn tồn kho xong (compensate Complete thành công)
    /// -> Saga tiếp tục gửi CancelOrderCommand (LIFO).
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Timestamp"></param>
    public record InventoryReleasedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ):IOrderEvent;
}
