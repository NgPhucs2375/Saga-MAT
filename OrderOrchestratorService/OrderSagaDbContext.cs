using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Domain.Entities;

namespace OrderOrchestratorService
{
    public class OrderSagaDbContext : DbContext
    {
        public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderStateMap).Assembly);
        }
    }
}   