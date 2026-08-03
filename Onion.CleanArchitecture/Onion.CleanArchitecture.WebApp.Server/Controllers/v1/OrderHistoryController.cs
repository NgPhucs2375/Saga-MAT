using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderHistory;
using System;

namespace Onion.CleanArchitecture.WebApp.Server.Controllers.v1
{
    [Authorize]
    [Route("api/orders/{orderId}/history")]
    public class OrderHistoryController : BaseApiController
    {
        [Obsolete]
        public OrderHistoryController(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {
        }

        // GET: api/orders/{orderId}/history
        [HttpGet]
        public async Task<IActionResult> GetHistory(Guid orderId)
        {
            return await EnforcePermissionAndExecute("orders", "show", async () =>
            {
                return Ok(await Mediator.Send(new GetOrderHistoryQuery { OrderId = orderId }));
            });
        }
    }
}