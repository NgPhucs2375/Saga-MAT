using MediatR;
using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetEventStores
{
    public class GetEventStoresQuery : IRequest<Response<object>>
    {
        public int _start { get; set; }
        public int _end { get; set; }
        public string _sort { get; set; }
        public string _order { get; set; }
        public List<string> _filter { get; set; }
    }

    public class GetEventStoresQueryHandler : IRequestHandler<GetEventStoresQuery, Response<object>>
    {
        private readonly IEventStoreRepositoryAsync _eventStoreRepository;

        public GetEventStoresQueryHandler(IEventStoreRepositoryAsync eventStoreRepository)
        {
            _eventStoreRepository = eventStoreRepository;
        }

        public async Task<Response<object>> Handle(GetEventStoresQuery request, CancellationToken ct)
        {
            var paged = await _eventStoreRepository.GetPagedFilteredAsync(new RequestParameter
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