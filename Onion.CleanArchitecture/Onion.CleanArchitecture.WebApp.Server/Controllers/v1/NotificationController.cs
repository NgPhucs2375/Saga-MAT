using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Onion.CleanArchitecture.Application.Features.Notifications.Queries.GetNotifications;
using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

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
        // Client gửi theo convention _filter=OrderId:guid (như các resource khác)
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool? isRead = null, [FromQuery] Guid? orderId = null, [FromQuery] RequestParameter filter = null)
        {
            return await EnforcePermissionAndExecute("notifications", "list", async () =>
            {
                if (orderId == null && filter?._filter != null)
                {
                    var orderFilter = filter._filter
                        .FirstOrDefault(f => f.StartsWith("OrderId:", StringComparison.OrdinalIgnoreCase));
                    if (orderFilter != null &&
                        Guid.TryParse(orderFilter.Substring("OrderId:".Length), out var parsedOrderId))
                    {
                        orderId = parsedOrderId;
                    }
                }

                var result = await Mediator.Send(new GetNotificationsQuery { IsRead = isRead, OrderId = orderId });
                var items = result.Data ?? new List<Notification>();
                return Ok(new Response<object>(new
                {
                    _start = filter?._start ?? 0,
                    _end = filter?._end ?? items.Count,
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