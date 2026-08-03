using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderOrchestratorService
{
    public class OrderSagaDesignTimeDbContextFactory
        : IDesignTimeDbContextFactory<OrderSagaDbContext>
    {
        public OrderSagaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderSagaDbContext>();
            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=LearnSaga;Username=postgres;Password=2375");
            return new OrderSagaDbContext(optionsBuilder.Options);
        }
    }
}