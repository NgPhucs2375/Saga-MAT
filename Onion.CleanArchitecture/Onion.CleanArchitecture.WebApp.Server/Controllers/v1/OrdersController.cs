using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.WebApp.Server.Controllers.v1
{
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : BaseApiController
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        [Obsolete]
        public OrdersController(
            Microsoft.AspNetCore.Hosting.IHostingEnvironment hostingEnvironment,
            IOrderRepositoryAsync orderRepository,
            IProductRepositoryAsync productRepository,
            IPublishEndpoint publishEndpoint,
            IAuthenticatedUserService authenticatedUserService) : base(hostingEnvironment)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _publishEndpoint = publishEndpoint;
            _authenticatedUserService = authenticatedUserService;
        }

        // POST api/orders
        [HttpPost]
        public async Task<IActionResult> Post(CreateOrderRequest request)
        {
            return await EnforcePermissionAndExecute("orders", "create", async () =>
            {
                if (request == null || request.Items == null || !request.Items.Any())
                {
                    return BadRequest(new Onion.CleanArchitecture.Application.Wrappers.Response<string>("Yêu cầu đặt hàng không hợp lệ."));
                }

                var orderId = Guid.NewGuid();
                var customerIdStr = _authenticatedUserService.UserId;
                if (string.IsNullOrEmpty(customerIdStr))
                {
                    return Unauthorized(new Onion.CleanArchitecture.Application.Wrappers.Response<string>("Không xác định được danh tính người dùng."));
                }

                Guid customerIdGuid;
                if (!Guid.TryParse(customerIdStr, out customerIdGuid))
                {
                    customerIdGuid = Guid.Empty;
                }

                var orderItems = new List<OrderItem>();
                decimal totalAmount = 0;

                foreach (var item in request.Items)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product == null)
                    {
                        return BadRequest(new Onion.CleanArchitecture.Application.Wrappers.Response<string>($"Sản phẩm có ID {item.ProductId} không tồn tại."));
                    }

                    if (!product.IsActive)
                    {
                        return BadRequest(new Onion.CleanArchitecture.Application.Wrappers.Response<string>($"Sản phẩm \"{product.Name}\" đã bị vô hiệu hóa."));
                    }

                    var orderItem = new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    };

                    orderItems.Add(orderItem);
                    totalAmount += orderItem.Quantity * orderItem.UnitPrice;
                }

                var order = new Order
                {
                    OrderId = orderId,
                    OrderCode = "ORD-" + DateTime.UtcNow.Ticks.ToString().Substring(10),
                    CustomerId = customerIdStr,
                    Status = OrderStatus.Submitted,
                    TotalAmount = totalAmount,
                    ShippingAddress = request.ShippingAddress,
                    Note = request.Note,
                    OrderItems = orderItems
                };

                // Lưu vào database (AddAsync tự động gọi SaveChangesAsync)
                await _orderRepository.AddAsync(order);

                // Publish Event để bắt đầu Saga
                var eventItems = orderItems.Select(oi => new OrderItemDto(oi.ProductId, oi.Quantity, oi.UnitPrice)).ToList();
                var orderSubmittedEvent = new OrderSubmittedEvent(
                    Guid.NewGuid(),
                    orderId,
                    customerIdGuid,
                    eventItems,
                    totalAmount,
                    DateTime.UtcNow
                );

                await _publishEndpoint.Publish(orderSubmittedEvent);

                return Ok(new Onion.CleanArchitecture.Application.Wrappers.Response<Guid>(orderId, "Đặt đơn hàng thành công, tiến trình Saga đã bắt đầu."));
            });
        }
    }

    public class CreateOrderRequest
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
