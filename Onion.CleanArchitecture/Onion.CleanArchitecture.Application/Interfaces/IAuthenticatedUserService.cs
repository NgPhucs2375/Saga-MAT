using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.CleanArchitecture.Application.Interfaces
{
    public interface IAuthenticatedUserService
    {
        string UserId { get; }
        bool IsSuperAdmin { get; }
    }
}
