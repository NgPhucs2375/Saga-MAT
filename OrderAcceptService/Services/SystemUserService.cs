using Onion.CleanArchitecture.Application.Interfaces;

namespace OrderAcceptService.Services
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
    }
}