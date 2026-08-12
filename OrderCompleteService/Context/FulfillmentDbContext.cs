

using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Entities;
using MassTransit;

namespace OrderCompleteService.Context
{
    public class FulfillmentDbContext : DbContext
    {
        private readonly IDateTimeService _dateTime;
        private readonly IAuthenticatedUserService _authenticatedUser;

        public FulfillmentDbContext(
            DbContextOptions<FulfillmentDbContext> options,
            IDateTimeService dateTime,
            IAuthenticatedUserService authenticatedUser) : base(options)
        {
            _dateTime = dateTime;
            _authenticatedUser = authenticatedUser;
        }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<CompletingRecord> CompletingRecords => Set<CompletingRecord>();

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = _dateTime.NowUtc;
                        entry.Entity.CreatedBy = _authenticatedUser.UserId;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedAt = _dateTime.NowUtc;
                        entry.Entity.LastModifiedBy = _authenticatedUser.UserId;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.AddTransactionalOutboxEntities();

            builder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Version).IsConcurrencyToken();
            });

            builder.Entity<CompletingRecord>(entity =>
            {
                entity.HasKey(e => e.RecordId);
                entity.Property(e => e.OrderId).IsRequired();
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            });

            base.OnModelCreating(builder);
        }
    }
}
