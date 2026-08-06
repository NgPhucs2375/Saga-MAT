using MediatR;
using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetOrderHistories
{
    public class GetOrderHistoriesQuery : IRequest<Response<object>>
    {
        public int _start { get; set; }
        public int _end { get; set; }
        public string _sort { get; set; }
        public string _order { get; set; }
        public List<string> _filter { get; set; }
    }

    public class GetOrderHistoriesQueryHandler : IRequestHandler<GetOrderHistoriesQuery, Response<object>>
    {
        private readonly IOrderHistoryRepositoryAsync _historyRepository;

        public GetOrderHistoriesQueryHandler(IOrderHistoryRepositoryAsync historyRepository)
        {
            _historyRepository = historyRepository;
        }

        public async Task<Response<object>> Handle(GetOrderHistoriesQuery request, CancellationToken ct)
        {
            var paged = await _historyRepository.GetPagedFilteredAsync(new RequestParameter
            {
                _start = request._start,
                _end = request._end,
                _sort = request._sort,
                _order = request._order,
                _filter = request._filter
            });

            return new Response<object>(new
            {
                paged._start,
                paged._end,
                paged._total,
                _data = paged
            }, "Success");
        }
    }
}