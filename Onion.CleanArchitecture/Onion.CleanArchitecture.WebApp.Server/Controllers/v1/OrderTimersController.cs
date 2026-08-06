using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderTimers;
using Onion.CleanArchitecture.Application.Filters;

namespace Onion.CleanArchitecture.WebApp.Server.Controllers.v1
{
    [Authorize]
    [Route("api/ordertimers")]
    public class OrderTimersController : BaseApiController
    {
        [Obsolete]
        public OrderTimersController(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {
        }

        // GET: api/ordertimers?_start=0&_end=5&_sort=Timeout&_order=desc&_filter=OrderId:guid
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] RequestParameter filter)
        {
            return await EnforcePermissionAndExecute("orders", "show", async () =>
            {
                return Ok(await Mediator.Send(new GetOrderTimersQuery
                {
                    _start = filter._start,
                    _end = filter._end,
                    _order = filter._order,
                    _sort = filter._sort,
                    _filter = filter._filter
                }));
            });
        }
    }
}