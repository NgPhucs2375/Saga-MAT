using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Onion.CleanArchitecture.Application.Features.Notifications.Queries.GetNotifications;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;

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

        // GET: api/notifications?isRead=false&orderId=xxx
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool? isRead = null, [FromQuery] Guid? orderId = null)
        {
            return await EnforcePermissionAndExecute("notifications", "list", async () =>
            {
                var result = await Mediator.Send(new GetNotificationsQuery { IsRead = isRead, OrderId = orderId });
                var items = result.Data ?? new List<Notification>();
                return Ok(new Response<object>(new
                {
                    _start = 0,
                    _end = items.Count,
                    _total = items.Count,
                    _data = items
                }, "Success"));
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