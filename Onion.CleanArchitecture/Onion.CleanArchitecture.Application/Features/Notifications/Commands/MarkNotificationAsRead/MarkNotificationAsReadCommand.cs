using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Notifications.Commands.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommand : IRequest<Response<Guid>>
    {
        public Guid NotifyId { get; set; }
    }

    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, Response<Guid>>
    {
        private readonly INotificationRepositoryAsync _notificationRepository;

        public MarkNotificationAsReadCommandHandler(INotificationRepositoryAsync notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<Response<Guid>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            await _notificationRepository.MarkAsReadAsync(request.NotifyId);

            return new Response<Guid>(request.NotifyId);
        }
    }
}