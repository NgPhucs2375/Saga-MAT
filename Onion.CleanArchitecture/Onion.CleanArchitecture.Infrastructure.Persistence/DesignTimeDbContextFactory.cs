using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared.Services;

namespace Onion.CleanArchitecture.Infrastructure.Persistence
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=LearnSaga;Username=postgres;Password=2375");

            return new ApplicationDbContext(
                optionsBuilder.Options,
                new DateTimeService(),
                new AuthenticatedUserServiceStub());
        }
    }

    internal class AuthenticatedUserServiceStub : IAuthenticatedUserService
    {
        public string UserId => "DesignTimeUser";
        public bool IsSuperAdmin => true;
    }
}
