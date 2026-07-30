using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Response khi Submit THÀNH CÔNG -> Chuyển tiếp sang OrderAcceptConsumer
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="Noti"></param>
    /// <param name="Timestamp"></param>
    public record OrderSubmitSuccessResponse(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        NotificationPayLoad Noti,
        DateTime Timestamp
    ):IOrderEvent;
}