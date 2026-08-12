using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders;
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
    public class OrderRepositoryAsync : GenericRepositoryAsync<Order>, IOrderRepositoryAsync
    {
        private readonly DbSet<Order> _orders;
        private readonly DbSet<OrderItem> _orderItems;

        public OrderRepositoryAsync(DbContext dbContext) : base(dbContext)
        {
            _orders = dbContext.Set<Order>();
            _orderItems = dbContext.Set<OrderItem>();
        }

        public async Task<Order> GetByIdAsync(Guid orderId)
        {
            return await _orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order> GetByCodeAsync(string orderCode)
        {
            return await _orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        }

        public async Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId)
        {
            return await _orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderItems)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _orders
                .Where(o => o.Status == status)
                .Include(o => o.OrderItems)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<OrderItem>> GetOrderItemsAsync(Guid orderId)
        {
            return await _orderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<PagedList<Order>> GetPagedOrdersAsync(GetAllOrdersParameter request)
        {
            var orderQuery = _orders.Include(o => o.OrderItems).AsQueryable();
            if (request._filter != null && request._filter.Count > 0)
            {
                orderQuery = MethodExtensions.ApplyFilters(orderQuery, request._filter);
            }

            return await PagedList<Order>.ToPagedList(orderQuery.OrderByDynamic(request._sort, request._order).AsNoTracking(), request._start, request._end);
        }

        public async Task<int> CountByStatusAsync(OrderStatus status)
        {
            return await _orders.CountAsync(o => o.Status == status);
        }

        public async Task<decimal> SumTotalAmountByStatusAsync(OrderStatus status)
        {
            return await _orders.Where(o => o.Status == status).SumAsync(o => o.TotalAmount);
        }
    }
}