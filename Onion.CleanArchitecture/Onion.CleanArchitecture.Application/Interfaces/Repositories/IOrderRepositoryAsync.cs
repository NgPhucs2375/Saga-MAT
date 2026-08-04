using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;

namespace Onion.CleanArchitecture.Application.Interfaces.Repositories{
    public interface IOrderRepositoryAsync : IGenericRepositoryAsync<Order>
    {
        Task<Order> GetByIdAsync(Guid orderId);                 // nạp kèm OrderItems
        Task<Order> GetByCodeAsync(string orderCode);
        Task<IReadOnlyList<Order>> GetByCustomerAsync(string customerId);
        Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status);
        Task<IReadOnlyList<OrderItem>> GetOrderItemsAsync(Guid orderId);
        Task<PagedList<Order>> GetPagedOrdersAsync(GetAllOrdersParameter parameter);
        Task<int> CountByStatusAsync(OrderStatus status);
        Task<decimal> SumTotalAmountByStatusAsync(OrderStatus status);
    }
}