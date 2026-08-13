using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Events
{

    public record OrderStatusUpdatedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        DateTime Timestamp
    ):IOrderEvent;
}
