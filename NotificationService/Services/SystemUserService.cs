using Onion.CleanArchitecture.Application.Interfaces;

namespace NotificationService.Services
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
    }
}
