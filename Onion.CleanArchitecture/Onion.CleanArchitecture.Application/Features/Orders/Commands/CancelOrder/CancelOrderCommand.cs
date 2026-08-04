using MediatR;
using MassTransit;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;

using Onion.CleanArchitecture.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using AppWrappers = Onion.CleanArchitecture.Application.Wrappers;

namespace Onion.CleanArchitecture.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommand : IRequest<AppWrappers.Response<Guid>>
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; }
    }

    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand,AppWrappers.Response<Guid>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public CancelOrderCommandHandler(IOrderRepositoryAsync orderRepository, IPublishEndpoint publishEndpoint)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<AppWrappers.Response<Guid>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                throw new ApiException("Order Not Found.");
            }

            // Chỉ hủy được khi đơn đang ở trạng thái Accepted
            // (khớp idempotency guard của CancelOrderConsumer trong OrderAcceptService).
            if (order.Status != OrderStatus.Accepted)
            {
                throw new ApiException($"Không thể hủy đơn hàng ở trạng thái {order.Status}. Chỉ hủy được khi đơn đang được duyệt (Accepted).");
            }

            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Hủy bởi người dùng từ WebApp"
                : request.Reason;

            // Gửi CancelOrderCommand (Domain.Events) tới OrderAcceptService qua Message Broker.
            // CancelOrderConsumer sẽ cập nhật Order Accepted -> Rejected, vô hiệu hóa timer,
            // rồi publish OrderCancelledEvent để Saga chuyển trạng thái Cancelled + thông báo UI.
            // Handler KHÔNG tự sửa trạng thái Order trực tiếp để tránh trùng logic với consumer.
            await _publishEndpoint.Publish(new Onion.CleanArchitecture.Domain.Events.CancelOrderCommand(
                Guid.NewGuid(),                        // EventId
                order.OrderId,                         // OrderId
                Guid.Parse(order.CustomerId),          // CustomerId (string -> Guid)
                reason,                                // Reason
                DateTime.UtcNow                        // Timestamp
            ), cancellationToken);

            return new AppWrappers.Response<Guid>(order.OrderId);
        }
    }
}