using MediatR;
using MassTransit;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using AppWrappers = Onion.CleanArchitecture.Application.Wrappers;

namespace Onion.CleanArchitecture.Application.Features.Orders.Commands.RejectOrder
{
    public class RejectOrderCommand : IRequest<AppWrappers.Response<Guid>>
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; }
    }

    public class RejectOrderCommandHandler : IRequestHandler<RejectOrderCommand, AppWrappers.Response<Guid>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public RejectOrderCommandHandler(IOrderRepositoryAsync orderRepository, IPublishEndpoint publishEndpoint)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<AppWrappers.Response<Guid>> Handle(RejectOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                throw new ApiException("Order Not Found.");
            }

            // Chỉ từ chối được khi đơn đang chờ duyệt
            // (khớp idempotency guard của RejectOrderConsumer trong OrderAcceptService).
            if (order.Status != OrderStatus.PendingApproval)
            {
                throw new ApiException($"Không thể từ chối đơn hàng ở trạng thái {order.Status}. Chỉ từ chối được khi đơn đang chờ duyệt (PendingApproval).");
            }

            var reason = string.IsNullOrWhiteSpace(request.Reason)
                ? "Từ chối bởi người duyệt từ WebApp"
                : request.Reason;

            // Gửi RejectOrderCommand (Domain.Events) tới OrderAcceptService qua Message Broker.
            // RejectOrderConsumer sẽ publish OrderAcceptFailedEvent -> Saga bồi hoàn LIFO -> Rejected.
            await _publishEndpoint.Publish(new Onion.CleanArchitecture.Domain.Events.RejectRequestedEvent(
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