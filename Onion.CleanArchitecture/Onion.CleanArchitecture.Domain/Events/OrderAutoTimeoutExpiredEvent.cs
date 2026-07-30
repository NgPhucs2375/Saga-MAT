using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Event tự động kích hoạt bởi Timer Job sau N phút nếu không có ai thao tác
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="TargetAction"></param>
    /// <param name="Timestamp"></param>
    public record OrderAutoTimeoutExpiredEvent(
        Guid EventId,
        Guid OrderId,
        string TargetAction,
        DateTime Timestamp
    ):IOrderEvent;
}