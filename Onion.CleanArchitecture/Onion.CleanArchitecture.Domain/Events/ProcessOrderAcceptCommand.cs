using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Command/Event để kích hoạt OrderAcceptConsumer
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Timestamp"></param>
    public record ProcessOrderAcceptCommand(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ) : IOrderEvent;

}