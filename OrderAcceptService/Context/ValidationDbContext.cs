

using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Entities;
using MassTransit;

namespace OrderAcceptService.Context
{
    public class ValidationDbContext : DbContext
    {
        private readonly IDateTimeService _dateTime;
        private readonly IAuthenticatedUserService _authenticatedUser;

        public ValidationDbContext(
            DbContextOptions<ValidationDbContext> options,
            IDateTimeService dateTime,
            IAuthenticatedUserService authenticatedUser) : base(options)
        {
            _dateTime = dateTime;
            _authenticatedUser = authenticatedUser;
        }

        public DbSet<AcceptRecord> ValidationRecords => Set<AcceptRecord>();
        public DbSet<OrderTimer> OrderTimers => Set<OrderTimer>();

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

            builder.Entity<AcceptRecord>(entity =>
            {
                entity.HasKey(e => e.RecordId);
                entity.Property(e => e.OrderId).IsRequired();
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            });

            builder.Entity<OrderTimer   >(entity =>
            {
                entity.HasKey(e => e.TimerId);
                entity.Property(e => e.OrderId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.TimerStatus).IsRequired();
            });

            base.OnModelCreating(builder);
        }
    }
}
