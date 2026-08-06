using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderHistories;
using Onion.CleanArchitecture.Application.Filters;

namespace Onion.CleanArchitecture.WebApp.Server.Controllers.v1
{
    [Authorize]
    [Route("api/orderhistories")]
    public class OrderHistoriesController : BaseApiController
    {
        [Obsolete]
        public OrderHistoriesController(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {
        }

        // GET: api/orderhistories?_start=0&_end=100&_sort=CreatedAt&_order=asc&_filter=OrderId:guid
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] RequestParameter filter)
        {
            return await EnforcePermissionAndExecute("orders", "show", async () =>
            {
                return Ok(await Mediator.Send(new GetOrderHistoriesQuery
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