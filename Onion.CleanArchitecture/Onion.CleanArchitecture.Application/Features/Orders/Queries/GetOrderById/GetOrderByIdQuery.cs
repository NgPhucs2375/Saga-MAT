using MediatR;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQuery: IRequest<Response<Order>>
    {
        public Guid Id { get; set; }
        public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Response<Order>>
        {
            private readonly IOrderRepositoryAsync _orderRepository;
            public GetOrderByIdQueryHandler(IOrderRepositoryAsync orderRepository)
            {
                _orderRepository = orderRepository;
            }
            public async Task<Response<Order>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
            {
                var order = await _orderRepository.GetByIdAsync(query.Id);
                if (order == null) throw new ApiException($"Order Not Found.");
                return new Response<Order>(order);
            }
        }
    }
}