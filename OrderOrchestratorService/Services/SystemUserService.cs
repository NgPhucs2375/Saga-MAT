using Onion.CleanArchitecture.Application.Interfaces;

namespace OrderOrchestratorService.Services
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
    }
}