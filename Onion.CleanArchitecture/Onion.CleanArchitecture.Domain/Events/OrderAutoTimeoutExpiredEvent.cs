using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event báo timer hết hạn -> OrderAcceptService tự động Reject/Complete
    /// theo TargetAction (khác với OrderTimeoutExpiredEvent dành cho Saga compensate).
    /// </summary>
    /// <param name="CorrelationId"></param>
    /// <param name="OrderId"></param>
    /// <param name="TargetAction"></param>
    /// <param name="Timestamp"></param>
    public record OrderAutoTimeoutExpiredEvent(
        Guid CorrelationId,
        Guid OrderId,
        string TargetAction,
        DateTime Timestamp
    );
}