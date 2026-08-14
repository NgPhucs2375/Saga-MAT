using Onion.CleanArchitecture.Application.Interfaces;

namespace OrderCompleteService.Services
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
         public bool IsSuperAdmin => true;
    }
}