using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event báo timer hết hạn -> Saga (DuringAny) gửi CancelOrderCommand (compensate)
    /// </summary>
    /// <param name="CorrelationId"></param>
    /// <param name="OrderId"></param>
    /// <param name="Timestamp"></param>
    public record OrderTimeoutExpiredEvent(
        Guid CorrelationId,
        Guid OrderId,
        DateTime Timestamp
    );
}
