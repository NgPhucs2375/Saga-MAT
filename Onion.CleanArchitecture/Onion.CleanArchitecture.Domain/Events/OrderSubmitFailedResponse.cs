using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// Response khi Submit THẤT BẠI (Sản phẩm không tồn tại / Kho không đủ)
    /// </summary>
    /// <param name="EventId"></param>
    /// <param name="OrderId"></param>
    /// <param name="CustomerId"></param>
    /// <param name="ErrorMessage"></param>
    /// <param name="Noti"></param>
    /// <param name="Timestamp"></param>
    public record OrderSubmitFailedResponse(
        Guid EventId,
        Guid OrderId,
        Guid CustomerId,
        string ErrorMessage,
        NotificationPayLoad Noti,
        DateTime Timestamp
    ):IOrderEvent;
}