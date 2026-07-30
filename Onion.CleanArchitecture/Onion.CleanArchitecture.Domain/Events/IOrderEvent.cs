using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    public interface IOrderEvent
    {
        Guid EventId { get; }
        Guid OrderId { get; }
        DateTime Timestamp { get; }
    }
}