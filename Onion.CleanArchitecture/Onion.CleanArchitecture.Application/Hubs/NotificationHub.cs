using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Onion.CleanArchitecture.Application.Hubs
{
    /// <summary>
    /// Hub đẩy NotificationPayLoad real-time về UI Client.
    /// Client gọi Subscribe(userId) để nhận thông báo riêng của user đó.
    /// </summary>
   
    public class NotificationHub : Hub
    {
        // Client đăng ký vào group của riêng mình để nhận notification theo TargetUserId
        public async Task Subscribe(Guid userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        }

        public async Task Unsubscribe(Guid userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
        }
    }
}
