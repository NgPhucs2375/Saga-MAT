using System;
using Onion.CleanArchitecture.Domain.Events;
namespace Onion.CleanArchitecture.Domain.Events
{
    public record OrderApprovalTimeoutExpiredEvent(
    Guid EventId,
    Guid OrderId,
    DateTime Timestamp
    ) : IOrderEvent;
}
