using MediatR;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IPublishEndpoint = MassTransit.IPublishEndpoint;

namespace Onion.CleanArchitecture.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommand : IRequest<Response<Guid>>
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public List<CreateOrderItemCommand> Items { get; set; } = new();
    }

    public class CreateOrderItemCommand
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Response<Guid>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public CreateOrderCommandHandler(
            IOrderRepositoryAsync orderRepository,
            IProductRepositoryAsync productRepository,
            IPublishEndpoint publishEndpoint,
            IAuthenticatedUserService authenticatedUserService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _publishEndpoint = publishEndpoint;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<Response<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            if (request == null || request.Items == null || !request.Items.Any())
            {
                return new Response<Guid>(false, default, "Yêu cầu đặt hàng không hợp lệ.");
            }

            var orderId = Guid.NewGuid();
            var customerIdStr = _authenticatedUserService.UserId;
            if (string.IsNullOrEmpty(customerIdStr))
            {
                return new Response<Guid>(false, default, "Không xác định được danh tính người dùng.");
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
                    return new Response<Guid>(false, default, $"Sản phẩm có ID {item.ProductId} không tồn tại.");
                }

                if (!product.IsActive)
                {
                    return new Response<Guid>(false, default, $"Sản phẩm \"{product.Name}\" đã bị vô hiệu hóa.");
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
            var orderCreatedEvent = new OrderCreatedEvent(
                Guid.NewGuid(),
                orderId,
                customerIdGuid,
                eventItems,
                totalAmount,
                DateTime.UtcNow
            );

            await _publishEndpoint.Publish(orderCreatedEvent);

            return new Response<Guid>(orderId, "Đặt đơn hàng thành công, tiến trình Saga đã bắt đầu.");
        }
    }
}