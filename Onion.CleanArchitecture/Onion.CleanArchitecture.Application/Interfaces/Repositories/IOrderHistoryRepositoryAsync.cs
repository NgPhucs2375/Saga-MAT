using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Interfaces.Repositories
{
    public interface IOrderHistoryRepositoryAsync : IGenericRepositoryAsync<OrderHistory>
    {
        Task<IReadOnlyList<OrderHistory>> GetByOrderIdAsync(Guid orderId);
        Task<bool> HasConsumerProcessedAsync(Guid orderId, string consumerName); // chống xử lý trùng (idempotency)
        Task<PagedList<OrderHistory>> GetPagedFilteredAsync(RequestParameter request);
    }
}