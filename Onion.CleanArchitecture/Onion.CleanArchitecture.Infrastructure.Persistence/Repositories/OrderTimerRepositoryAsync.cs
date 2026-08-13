using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Persistence.Repository;
using Onion.CleanArchitecture.Infrastructure.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Infrastructure.Persistence.Repositories
{
    public class OrderTimerRepositoryAsync : GenericRepositoryAsync<OrderTimer>, IOrderTimerRepositoryAsync
    {
        private readonly DbSet<OrderTimer> _orderTimers;

        public OrderTimerRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _orderTimers = dbContext.Set<OrderTimer>();
        }

        public async Task<OrderTimer> GetPendingByOrderIdAsync(Guid orderId)
        {
            return await _orderTimers
                .FirstOrDefaultAsync(t => t.OrderId == orderId && t.TimerStatus == TimerStatus.Pending);
        }

        public async Task<PagedList<OrderTimer>> GetPagedFilteredAsync(RequestParameter request)
        {
            var query = _orderTimers.AsQueryable();
            if (request._filter != null && request._filter.Count > 0)
            {
                query = MethodExtensions.ApplyFilters(query, request._filter);
            }

            return await PagedList<OrderTimer>.ToPagedList(
                query.OrderByDynamic(request._sort, request._order).AsNoTracking(),
                request._start, request._end);
        }
    }
}