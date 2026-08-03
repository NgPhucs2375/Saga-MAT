using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Features.Orders.Commands.CreateOrder;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderById;

namespace Onion.CleanArchitecture.WebApp.Server.Controllers.v1
{
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : BaseApiController
    {
        [Obsolete]
        public OrdersController(Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {
        }

        // GET: api/orders?_start=0&_end=10&_order=asc&_sort=OrderCode
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetAllOrdersParameter filter)
        {
            return await EnforcePermissionAndExecute("orders", "list", async () =>
            {
                return Ok(await Mediator.Send(new GetAllOrdersQuery()
                {
                    _end = filter._end,
                    _start = filter._start,
                    _order = filter._order,
                    _sort = filter._sort,
                    _filter = filter._filter
                }));
            });
        }

        // GET: api/orders/show/{id}
        [HttpGet("show/{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            return await EnforcePermissionAndExecute("orders", "show", async () =>
            {
                return Ok(await Mediator.Send(new GetOrderByIdQuery { Id = id }));
            });
        }

        // POST: api/orders
        [HttpPost]
        public async Task<IActionResult> Post(CreateOrderCommand command)
        {
            return await EnforcePermissionAndExecute("orders", "create", async () =>
            {
                return Ok(await Mediator.Send(command));
            });
        }
    }
}