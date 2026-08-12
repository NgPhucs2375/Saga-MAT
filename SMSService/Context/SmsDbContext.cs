

using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Entities;
using MassTransit;

namespace SMSService.Context
{
    public class SmsDbContext : DbContext
    {
        private readonly IDateTimeService _dateTime;
        private readonly IAuthenticatedUserService _authenticatedUser;

        public SmsDbContext(
            DbContextOptions<SmsDbContext> options,
            IDateTimeService dateTime,
            IAuthenticatedUserService authenticatedUser) : base(options)
        {
            _dateTime = dateTime;
            _authenticatedUser = authenticatedUser;
        }

        public DbSet<SmsLog> SmsLogs => Set<SmsLog>();
        public DbSet<EventStore> EventStores => Set<EventStore>();

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

            builder.Entity<SmsLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Content).HasMaxLength(500);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.ProviderResponse).HasMaxLength(500);
            });

            builder.Entity<EventStore>(entity =>
            {
                entity.HasKey(e => e.StoreId);
                entity.Property(e => e.EventType).HasMaxLength(200);
                entity.Property(e => e.PayLoad).HasColumnType("text");
                entity.Property(e => e.CorrelationId).IsRequired();
            });

            base.OnModelCreating(builder);
        }
    }
}
