using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
            // Tích hợp các bảng Outbox/Inbox/OutboxState của MassTransit vào DbContext
            modelBuilder.AddTransactionalOutboxEntities();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Một số migration/snapshot được sinh bởi EF tool 10 (WSL) trong khi runtime là EF 9,
            // gây PendingModelChangesWarning -> Migrate() ném exception chặn Saga khởi động.
            // Đã loại bỏ các cột thừa (ApprovalRequestorId/ApprovalRequestedAt) trong OrderState,
            // nên model đã khớp schema; bỏ qua warning này để Demo không bị crash.
            optionsBuilder.ConfigureWarnings(w => w
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}
