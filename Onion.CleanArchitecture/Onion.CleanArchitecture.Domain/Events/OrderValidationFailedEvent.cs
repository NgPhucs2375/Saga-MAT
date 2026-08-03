using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event thông báo Validate THẤT BẠI -> Saga chuyển Rejected
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorMessage"></param>
    /// <param name="Timestamp"></param>
    public record OrderValidationFailedEvent(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorMessage,
        DateTime Timestamp
    ):IOrderEvent;
}
