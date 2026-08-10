using MediatR;
using MassTransit;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using AppWrappers = Onion.CleanArchitecture.Application.Wrappers;

namespace Onion.CleanArchitecture.Application.Features.Orders.Commands.ApproveOrder
{
    public class ApproveOrderCommand : IRequest<AppWrappers.Response<Guid>>
    {
        public Guid OrderId { get; set; }
    }

    public class ApproveOrderCommandHandler : IRequestHandler<ApproveOrderCommand, AppWrappers.Response<Guid>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public ApproveOrderCommandHandler(IOrderRepositoryAsync orderRepository, IPublishEndpoint publishEndpoint)
        {
            _orderRepository = orderRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<AppWrappers.Response<Guid>> Handle(ApproveOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                throw new ApiException("Order Not Found.");
            }

            // Chỉ duyệt được khi đơn đang chờ duyệt
            // (khớp idempotency guard của ApproveOrderConsumer trong OrderAcceptService).
            if (order.Status != OrderStatus.PendingApproval)
            {
                throw new ApiException($"Không thể duyệt đơn hàng ở trạng thái {order.Status}. Chỉ duyệt được khi đơn đang chờ duyệt (PendingApproval).");
            }

            // Gửi ApproveOrderCommand (Domain.Events) tới OrderAcceptService qua Message Broker.
            // ApproveOrderConsumer sẽ đổi PendingApproval -> Accepted, vô hiệu hóa timer,
            // rồi publish OrderAcceptedEvent để Saga chuyển Completing + gửi CompleteOrderCommand.
            await _publishEndpoint.Publish(new Onion.CleanArchitecture.Domain.Events.ApproveRequestedEvent(
                Guid.NewGuid(),                        // EventId
                order.OrderId,                         // OrderId
                Guid.Parse(order.CustomerId),          // CustomerId (string -> Guid)
                DateTime.UtcNow                        // Timestamp
            ), cancellationToken);

            return new AppWrappers.Response<Guid>(order.OrderId);
        }
    }
}