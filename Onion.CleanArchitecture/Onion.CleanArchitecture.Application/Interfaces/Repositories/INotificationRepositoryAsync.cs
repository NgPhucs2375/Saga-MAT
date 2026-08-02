using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Interfaces.Repositories
{
    public interface INotificationRepositoryAsync : IGenericRepositoryAsync<Notification>
    {
        Task<IReadOnlyList<Notification>> GetByUserAsync(Guid targetUserId, bool? isRead = null);
        Task MarkAsReadAsync(Guid notifyId);
    }
}