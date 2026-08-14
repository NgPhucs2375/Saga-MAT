using Onion.CleanArchitecture.Application.Interfaces;
using System;
using System.Security.Claims;

namespace Onion.CleanArchitecture.WebApp.Server.Services
{
    public class AuthenticatedUserService : IAuthenticatedUserService
    {
        public AuthenticatedUserService(IHttpContextAccessor httpContextAccessor)
        {
            UserId = httpContextAccessor.HttpContext?.User?.FindFirstValue("uid");
            IsSuperAdmin = string.Equals(
                httpContextAccessor.HttpContext?.User?.FindFirstValue("permission"),
                "SuperAdmin",
                StringComparison.OrdinalIgnoreCase);
        }

        public string UserId { get; } = string.Empty;
        public bool IsSuperAdmin { get; }
    }
}
