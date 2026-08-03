using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace OrderOrchestration
{
    /// <summary>
    /// DbContext chuyên cho saga (Npgsql), map OrderState -> bảng OrderState.
    /// Dùng để tạo migration + lưu saga instance để resume sau crash.
    /// </summary>
    public class OrderSagaDbContext : SagaDbContext
    {
        public OrderSagaDbContext(DbContextOptions<OrderSagaDbContext> options) : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new OrderStateMap(); }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
