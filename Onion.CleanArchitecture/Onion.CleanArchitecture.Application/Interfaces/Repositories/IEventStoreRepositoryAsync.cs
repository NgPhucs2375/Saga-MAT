using Onion.CleanArchitecture.Application.Filters;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Interfaces.Repositories
{
    public interface IEventStoreRepositoryAsync : IGenericRepositoryAsync<EventStore>
    {
        Task<PagedList<EventStore>> GetPagedFilteredAsync(RequestParameter request);
    }
}
