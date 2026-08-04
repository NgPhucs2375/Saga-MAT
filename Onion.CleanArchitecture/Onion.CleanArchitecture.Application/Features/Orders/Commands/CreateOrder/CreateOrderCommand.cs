using MassTransit;
using MediatR;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Domain.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Apllication.Features.Orders.Commands.CreateOrder
{
    /// <summary>
    /// Item do client gửi lên: chỉ cần ProductId + Quantity.
    /// Giá (UnitPrice), tên sản phẩm, TotalAmount được BE tự tính từ Product để tránh tin client.
    /// </summary>
    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public partial class CreateOrderCommand : IRequest<Application.Wrappers.Response<int>>
    {
        public string ShippingAddress { get; set; }
        public string Note { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; }
    }

    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Application.Wrappers.Response<int>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IAuthenticatedUserService _authenticatedUser;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreateOrderCommandHandler(
            IOrderRepositoryAsync orderRepository,
            IProductRepositoryAsync productRepository,
            IAuthenticatedUserService authenticatedUser,
            IPublishEndpoint publishEndpoint)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _authenticatedUser = authenticatedUser;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<Application.Wrappers.Response<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            // 1. Build OrderItems từ Items (client gửi ProductId + Quantity), lấy giá/tên từ Product
            var orderItems = new List<OrderItem>();
            var orderItemDtos = new List<OrderItemDto>();
            var totalAmount = 0m;

            if (request.Items != null)
            {
                foreach (var item in request.Items)
                {
                    var product = await _productRepository.GetProductByIdAsync(item.ProductId);
                    if (product == null || !product.IsActive)
                        continue;

                    // Kiểm tra tồn kho: số lượng theo yêu cầu phải <= SLTKho hiện tại
                    if (item.Quantity <= 0 || item.Quantity > product.SLTKho)
                    {
                        return new Application.Wrappers.Response<int>
                        {
                            Succeeded = false,
                            Code = -1,
                            Message = $"Sản phẩm '{product.Name}' chỉ còn {product.SLTKho} trong kho."
                        };
                    }

                    orderItems.Add(new OrderItem
                    {
                        OrderItemId = Guid.NewGuid(),
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    });
                    orderItemDtos.Add(new OrderItemDto(product.ProductId, item.Quantity, product.Price));
                    totalAmount += product.Price * item.Quantity;
                }
            }

            // 2. CustomerId lấy từ user đã đăng nhập (claim "uid" của AuthenticatedUserService)
            Guid.TryParse(_authenticatedUser.UserId, out var customerGuid);

            // 3. Khởi tạo Order (OrderCode tự sinh để đảm bảo unique, không cần client cung cấp)
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",
                CustomerId = customerGuid.ToString(),
                Status = OrderStatus.Submitted,
                TotalAmount = totalAmount,
                ShippingAddress = request.ShippingAddress,
                Note = request.Note,
                OrderItems = orderItems
            };

            // Gán OrderId cho từng OrderItem (FK)
            foreach (var item in order.OrderItems)
            {
                item.OrderId = order.OrderId;
            }

            // 4. Lưu vào cơ sở dữ liệu
            await _orderRepository.AddAsync(order);

            // 5. Phát sự kiện tới Message Broker (kích hoạt Saga)
            await _publishEndpoint.Publish(new OrderCreatedEvent(
                Guid.NewGuid(),                             // EventId
                order.OrderId,                              // OrderId
                customerGuid,                               // CustomerId
                orderItemDtos,                              // Items
                totalAmount,                                // TotalAmount
                DateTime.UtcNow                             // Timestamp
            ), cancellationToken);

            return new Application.Wrappers.Response<int>(order.Id);
        }
    }
}
