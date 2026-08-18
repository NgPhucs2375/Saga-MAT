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
    public class NotificationRepositoryAsync : GenericRepositoryAsync<Notification>, INotificationRepositoryAsync
    {
        private readonly DbSet<Notification> _notifications;

        public NotificationRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _notifications = dbContext.Set<Notification>();
        }

        public async Task<IReadOnlyList<Notification>> GetByUserAsync(Guid targetUserId, bool? isRead = null)
        {
            var query = _notifications.Where(n => n.TargetUserId == targetUserId);
            if (isRead.HasValue)
                query = query.Where(n => n.IsRead == isRead.Value);
            return await query.OrderByDescending(n => n.SendAt).ToListAsync();
        }

        public async Task MarkAsReadAsync(Guid notifyId)
        {
            var noti = await _notifications.FirstOrDefaultAsync(n => n.NotifyId == notifyId);
            if (noti == null) return;
            noti.IsRead = true;
            await base.UpdateAsync(noti);
        }

        public async Task<Notification> GetByIdAsync(Guid NotifyId)
        {
            return await _notifications.FirstOrDefaultAsync(n => n.NotifyId == NotifyId);
        }
    }
}