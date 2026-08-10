using System;

namespace Onion.CleanArchitecture.Domain.Events
{

    public record ApproveRequestedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ) : IOrderEvent;
}