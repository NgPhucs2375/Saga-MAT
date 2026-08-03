using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderOrchestration
{
    /// <summary>
    /// Factory để tạo migration cho OrderSagaDbContext:
    /// dotnet ef migrations add InitialOrderState -p OrderOrchestration
    /// </summary>
    public class OrderSagaDbContextFactory : IDesignTimeDbContextFactory<OrderSagaDbContext>
    {
        public OrderSagaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderSagaDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=LearnSaga;Username=postgres;Password=2375");

            return new OrderSagaDbContext(optionsBuilder.Options);
        }
    }
}
