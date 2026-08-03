using AutoMapper;
using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders
{
    public class GetAllOrdersQuery : IRequest<Response<object>>
    {
        public int _start { get; set; }
        public int _end { get; set; }
        public string _sort { get; set; }
        public string _order { get; set; }
        public List<string> _filter { get; set; }
    }

    public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Response<object>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IMapper _mapper;

        public GetAllOrdersQueryHandler(IOrderRepositoryAsync orderRepository, IMapper mapper)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
        }

        public async Task<Response<object>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
        {
            var validFilter = _mapper.Map<GetAllOrdersParameter>(request);
            var orders = await _orderRepository.GetPagedOrdersAsync(validFilter);
            return new Response<object>(true, new
            {
                orders._start,
                orders._end,
                orders._total,
                orders._hasNext,
                orders._hasPrevious,
                orders._pages,
                _data = orders
            }, message: "Success");
        }
    }
}