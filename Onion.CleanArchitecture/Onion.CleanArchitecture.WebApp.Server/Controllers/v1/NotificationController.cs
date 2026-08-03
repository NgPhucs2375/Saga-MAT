using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Onion.CleanArchitecture.Application.Features.Notifications.Queries.GetNotifications;
using System;

namespace Onion.CleanArchitecture.WebApp.Server.Controllers.v1
{
    [Authorize]
    [Route("api/notifications")]
    public class NotificationController : BaseApiController
    {
        [Obsolete]
        public NotificationController(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {
        }

        // GET: api/notifications?isRead=false
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool? isRead = null)
        {
            return await EnforcePermissionAndExecute("notifications", "list", async () =>
            {
                return Ok(await Mediator.Send(new GetNotificationsQuery { IsRead = isRead }));
            });
        }

        // PUT: api/notifications/{notifyId}/read
        [HttpPut("{notifyId}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notifyId)
        {
            return await EnforcePermissionAndExecute("notifications", "edit", async () =>
            {
                return Ok(await Mediator.Send(new MarkNotificationAsReadCommand { NotifyId = notifyId }));
            });
        }
    }
}