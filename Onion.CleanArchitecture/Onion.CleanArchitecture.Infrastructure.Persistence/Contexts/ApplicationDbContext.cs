using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Infrastructure.Persistence.Contexts
{
    /// <summary>
    /// nơi EF CORE "Nhìn thấy" entity nào để C/R dữ liệu trong DB
    /// 1.EF CORE map entity -> bảng qua  DbSet. Nếu không khai báo DbSet<Order>, EF không biết Order là một bảng → không thể context.Orders.ToListAsync(), và migration (dotnet ef migrations add) sẽ không tạo bảng Order/OrderItem/... trong DB.
    /// 2. Các Consumer cần DB. OrderAcceptConsumer/OrderCompleteConsumer phải load/update Order, trừ SLTKho của Product, ghi OrderHistory, tạo OrderTimer → tất cả cần có bảng → cần DbSet + migration.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        private readonly IDateTimeService _dateTime;
        private readonly IAuthenticatedUserService _authenticatedUser;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateTimeService dateTime, IAuthenticatedUserService authenticatedUser) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            _dateTime = dateTime;
            _authenticatedUser = authenticatedUser;
        }

        // 
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderHistory> OrderHistories => Set<OrderHistory>();
        public DbSet<OrderTimer> OrderTimers => Set<OrderTimer>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<EventStore> EventStores => Set<EventStore>();
        public DbSet<Product> Product => Set<Product>();


        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableBaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = _dateTime.Now;
                        entry.Entity.CreatedBy = _authenticatedUser.UserId;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedAt = _dateTime.Now;
                        entry.Entity.LastModifiedBy = _authenticatedUser.UserId;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            //All Decimals will have 18,6 Range
            foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,6)");
            }
            base.OnModelCreating(builder);
        }
    }
}
