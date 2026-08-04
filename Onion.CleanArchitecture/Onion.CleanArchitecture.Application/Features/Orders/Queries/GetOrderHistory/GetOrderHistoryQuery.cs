using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderHistory
{
    public class GetOrderHistoryQuery : IRequest<Response<IEnumerable<OrderHistory>>>
    {
        public Guid OrderId { get; set; }
    }

    public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, Response<IEnumerable<OrderHistory>>>
    {
        private readonly IOrderHistoryRepositoryAsync _historyRepository;

        public GetOrderHistoryQueryHandler(IOrderHistoryRepositoryAsync historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<Response<IEnumerable<OrderHistory>>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
        {
            var histories = await _historyRepository.GetByOrderIdAsync(request.OrderId);
            
            return new Response<IEnumerable<OrderHistory>>(histories);
        }
    }
}