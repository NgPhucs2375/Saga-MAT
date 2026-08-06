using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Persistence.Repository;
using Onion.CleanArchitecture.Infrastructure.Shared.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Infrastructure.Persistence.Repositories
{
    public class EventStoreRepositoryAsync : GenericRepositoryAsync<EventStore>, IEventStoreRepositoryAsync
    {
        private readonly DbSet<EventStore> _eventStores;

        public EventStoreRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _eventStores = dbContext.Set<EventStore>();
        }

        public async Task<PagedList<EventStore>> GetPagedFilteredAsync(RequestParameter request)
        {
            var query = _eventStores.AsQueryable();
            if (request._filter != null && request._filter.Count > 0)
            {
                query = MethodExtensions.ApplyFilters(query, request._filter);
            }

            return await PagedList<EventStore>.ToPagedList(
                query.OrderByDynamic(request._sort, request._order).AsNoTracking(),
                request._start, request._end);
        }
    }
}