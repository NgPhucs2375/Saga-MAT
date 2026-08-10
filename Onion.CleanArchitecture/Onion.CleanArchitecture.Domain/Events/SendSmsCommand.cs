using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    public record SendSmsCommand
    (
        Guid EventId,
        Guid OrderId,
        string PhoneNumber,
        string Content,
        DateTime Timestamp
    ): IOrderEvent;
}