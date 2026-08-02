using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Onion.CleanArchitecture.Infrastructure.Identity.Contexts;

namespace Onion.CleanArchitecture.Infrastructure.Identity
{
    public class IdentityContextFactory : IDesignTimeDbContextFactory<IdentityContext>
    {
        public IdentityContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=LearnSaga;Username=postgres;Password=2375");

            return new IdentityContext(optionsBuilder.Options);
        }
    }
}
