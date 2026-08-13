using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event thông báo Duyệt THÀNH CÔNG -> Kích hoạt OrderCompleteConsumer
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="AutoTimeoutMinutes"></param>
    /// <param name="Timestamp"></param>
    public record OrderAcceptedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ):IOrderEvent;
}