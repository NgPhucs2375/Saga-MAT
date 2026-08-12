using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    /// <summary>
    /// PayLoad dùng để chứa dữ liệu trả về cho UI qua SignalR
    /// </summary>
    /// <param name="TargetUserId"></param>
    /// <param name="Title"></param>
    /// <param name="Message"></param>
    /// <param name="NotificationType"></param>
    /// <param name="Timestamp"></param>
    public record NotificationPayLoad(
        Guid TargetUserId,
        string Title,
        string Message,
        string NotificationType, 
        Guid? OrderId,
        DateTime Timestamp
    );

}