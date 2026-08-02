using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Persistence.Repository;
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

        public async Task<IReadOnlyList<OrderTimer>> GetExpiredPendingAsync(DateTime now)
        {
            return await _orderTimers
                .Where(t => t.TimerStatus == TimerStatus.Pending && t.Timeout <= now)
                .ToListAsync();
        }
    }
}