using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Persistence.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Infrastructure.Persistence.Repositories
{
    public class OrderHistoryRepositoryAsync : GenericRepositoryAsync<OrderHistory>, IOrderHistoryRepositoryAsync
    {
        private readonly DbSet<OrderHistory> _orderHistories;

        public OrderHistoryRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _orderHistories = dbContext.Set<OrderHistory>();
        }

        public async Task<IReadOnlyList<OrderHistory>> GetByOrderIdAsync(Guid orderId)
        {
            return await _orderHistories
                .Where(h => h.OrderId == orderId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();
        }

        public Task<bool> HasConsumerProcessedAsync(Guid orderId, string consumerName)
        {
            return _orderHistories
                .AnyAsync(h => h.OrderId == orderId && h.ConsumerName == consumerName);
        }
    }
}