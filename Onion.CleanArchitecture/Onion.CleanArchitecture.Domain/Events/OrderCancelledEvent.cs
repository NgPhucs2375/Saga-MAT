using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// OrderAcceptService thông báo đã hủy THÀNH CÔNG -> Saga chuyển sang Rejected
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Reason"></param>
    /// <param name="Timestamp"></param>
     public record OrderCancelledEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string Reason,
        DateTime Timestamp
    ) : IOrderEvent;
}