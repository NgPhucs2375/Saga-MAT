using MediatR;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Notifications.Queries.GetNotifications
{
    public class GetNotificationsQuery : IRequest<Response<IReadOnlyList<Notification>>>
    {
        public bool? IsRead { get; set; }
        public Guid? OrderId { get; set; }
    }

    public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Response<IReadOnlyList<Notification>>>
    {
        private readonly INotificationRepositoryAsync _notificationRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetNotificationsQueryHandler(
            INotificationRepositoryAsync notificationRepository,
            IAuthenticatedUserService authenticatedUserService)
        {
            _notificationRepository = notificationRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<Response<IReadOnlyList<Notification>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            // Lấy TargetUserId (Guid) của user đang đăng nhập trên UI
            var targetUserId = Guid.Parse(_authenticatedUserService.UserId);

            var notifications = await _notificationRepository.GetByUserAsync(targetUserId, request.IsRead);

            if (request.OrderId.HasValue)
            {
                notifications = notifications.Where(n => n.OrderId == request.OrderId.Value).ToList();
            }

            return new Response<IReadOnlyList<Notification>>(notifications);
        }
    }
}