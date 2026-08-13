using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Interfaces.Repositories
{
    public interface IOrderTimerRepositoryAsync : IGenericRepositoryAsync<OrderTimer>
    {
        Task<OrderTimer> GetPendingByOrderIdAsync(Guid orderId);
        Task<PagedList<OrderTimer>> GetPagedFilteredAsync(RequestParameter request);
    }
}