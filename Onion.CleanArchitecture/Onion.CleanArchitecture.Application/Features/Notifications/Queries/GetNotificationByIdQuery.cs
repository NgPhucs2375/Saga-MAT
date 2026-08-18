using MediatR;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Notifications.Queries.GetNotificationById
{
    public class GetNotificationByIdQuery: IRequest<Response<Notification>>
    {
        public Guid Id { get; set; }
        public class GetNotificationByIdQueryHandler : IRequestHandler<GetNotificationByIdQuery, Response<Notification>>
        {
            private readonly INotificationRepositoryAsync _notificationRepository;
            private readonly IAuthenticatedUserService _authenticatedUser;
            public GetNotificationByIdQueryHandler(
                INotificationRepositoryAsync notificationRepository,
                IAuthenticatedUserService authenticatedUser)
            {
                _notificationRepository = notificationRepository;
                _authenticatedUser = authenticatedUser;
            }
            public async Task<Response<Notification>> Handle(GetNotificationByIdQuery query, CancellationToken cancellationToken)
            {
                var notification = await _notificationRepository.GetByIdAsync(query.Id);
                if (notification == null) throw new ApiException($"Notification Not Found.");
                // User thường chỉ xem được thông báo của chính mình; SuperAdmin xem tất cả
                if (!_authenticatedUser.IsSuperAdmin &&
                    !string.Equals(notification.TargetUserId.ToString(), _authenticatedUser.UserId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ApiException("Bạn không có quyền xem thông báo này.");
                }
                return new Response<Notification>(notification);
            }
        }
    }
}