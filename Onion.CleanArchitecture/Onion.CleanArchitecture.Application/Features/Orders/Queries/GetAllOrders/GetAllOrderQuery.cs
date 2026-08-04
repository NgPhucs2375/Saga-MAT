using AutoMapper;
using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders
{
    public class GetAllOrderQuery: IRequest<Response<object>>
    {
        public int _start { get; set; }
        public int _end { get; set; }
        public string _sort { get; set; }
        public string _order { get; set; }
        public List<string> _filter { get; set; }
    }
    public class GetAllOrderQueryHandler : IRequestHandler<GetAllOrderQuery,Response<object>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;
        private readonly IMapper _mapper;
        public GetAllOrderQueryHandler(
            IOrderRepositoryAsync orderRepository,
            IMapper mapper
        )
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
        }

        public async Task<Response<object>> Handle(GetAllOrderQuery request,CancellationToken ct)
        {
            var validFilter = _mapper.Map<GetAllOrdersParameter>(request);
            var pagedOrders = await _orderRepository.GetPagedOrdersAsync(validFilter);
            return new Response<object>(new
            {
                data = pagedOrders,
                total = pagedOrders._total
            }, "Success");
        }
    }
}