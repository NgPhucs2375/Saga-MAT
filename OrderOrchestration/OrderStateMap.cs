using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Onion.CleanArchitecture.Domain.Events;
using System.Text.Json;

namespace OrderOrchestration
{
    /// <summary>
    /// Cấu hình EF Core cho bảng OrderState (saga instance).
    /// </summary>
    public class OrderStateMap : SagaClassMap<OrderState>
    {
        protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
        {
            base.Configure(entity, model);

            entity.ToTable("OrderState");
            entity.HasKey(x => x.CorrelationId);

            entity.Property(x => x.CurrentState).HasMaxLength(64);
            entity.Property(x => x.CustomerId);
            entity.Property(x => x.TotalAmount).HasPrecision(18, 6);
            entity.Property(x => x.StepsCompleted);
            entity.Property(x => x.ErrorReason);
            entity.Property(x => x.Items)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<OrderItemDto>>(v, (JsonSerializerOptions)null));
            entity.Property(x => x.CreatedAt);
        }
    }
}
