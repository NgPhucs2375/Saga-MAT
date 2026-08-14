using Onion.CleanArchitecture.Application.Interfaces;

namespace SmsService.Services
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
         public bool IsSuperAdmin => true;
    }
}