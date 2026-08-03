using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// OrderSubmitService thông báo validate THẤT BẠI -> Saga reject đơn hàng
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
    ) : IOrderEvent;

}