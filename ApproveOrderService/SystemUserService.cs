using Onion.CleanArchitecture.Application.Interfaces;

namespace ApproveOrderService
{
    public class SystemUserService : IAuthenticatedUserService
    {
        public string UserId => "system";
    }
}