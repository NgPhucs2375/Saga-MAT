using AutoMapper;
using MediatR;
using Onion.CleanArchitecture.Application.Interfaces;
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
        private readonly IAuthenticatedUserService _authen;
        public GetAllOrderQueryHandler(
            IOrderRepositoryAsync orderRepository,
            IMapper mapper,
            IAuthenticatedUserService authen
        )
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
            _authen = authen;
        }

        public async Task<Response<object>> Handle(GetAllOrderQuery request,CancellationToken ct)
        {
            var validFilter = _mapper.Map<GetAllOrdersParameter>(request);
            // User thường chỉ được xem đơn của chính mình; SuperAdmin xem tất cả
            if (!_authen.IsSuperAdmin)
            {
                validFilter.CustomerId = _authen.UserId;
            }
            var pagedOrders = await _orderRepository.GetPagedOrdersAsync(validFilter);
            return new Response<object>(new
            {
                pagedOrders._start,
                pagedOrders._end,
                pagedOrders._total,
                pagedOrders._hasNext,
                pagedOrders._hasPrevious,
                pagedOrders._pages,
                _data = pagedOrders
            }, "Success");
        }
    }
}