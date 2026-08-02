using Onion.CleanArchitecture.Application.Interfaces;

namespace OrderSubmitService.Services
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
    }
}