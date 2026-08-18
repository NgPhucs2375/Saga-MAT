using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nest;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using OrderCompleteService;
using OrderCompleteService.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        //1. Đăng ký Provider trước khi AddNpgSqlPersistenceInfrastructure
        services.AddTransient<IDatabaseSettingsProvider,DatabaseSettingsProvider>();
        services.AddScoped<IAuthenticatedUserService, SystemUserService>();
        // DI ApplicationDbContext + Repositories + EF Core
        services.AddNpgSqlPersistenceInfrastructure();
        services.AddPersistenceRepositories();
        services.AddSharedInfrastructure(ctx.Configuration);
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = ctx.Configuration.GetConnectionString("PostgresConnection");
        });
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderCompleteConsumer>();
            x.AddConsumer<ReleaseInventoryConsumer>();
            x.SetKebabCaseEndpointNameFormatter();

            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.UsePostgres(); // Khai báo dùng PostgreSQL provider
                o.UseBusOutbox(); // Bật Outbox trên bus (tự động commit Outbox + publish event)
                o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30); // Cửa sổ chống trùng lặp Inbox
                // Tắt InboxCleanupService: tránh spam lỗi FK (InboxState bị xóa
                // khi OutboxMessage còn tham chiếu) ở phiên bản 8.3.0
                o.DisableInboxCleanupService();
            });

            x.UsingPostgres((context, cfg) =>
            {
                // Tự động khởi tạo schema/bảng queue trong PostgreSQL nếu chưa có
                cfg.AutoStart = true;

                // Nhận command từ Saga theo tên queue cố định
                cfg.ReceiveEndpoint("order-complete-queue", e =>
                {
                    // Retry in-process 3 lần cho các lỗi thoáng qua (DB connection, timeout)
                    e.UseMessageRetry(r => r.Exponential(
                        3, 
                        TimeSpan.FromSeconds(1), 
                        TimeSpan.FromSeconds(10), 
                        TimeSpan.FromSeconds(2)
                    ));

                    // Kết hợp Redelivery nếu lỗi kéo dài
                    e.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(5), 
                        TimeSpan.FromSeconds(15)
                    ));

                    e.ConfigureConsumer<OrderCompleteConsumer>(context);
                });

                cfg.ReceiveEndpoint("release-inventory-queue", e => {
                    // Retry in-process 3 lần cho các lỗi thoáng qua (DB connection, timeout)
                    e.UseMessageRetry(r => r.Exponential(
                        3, 
                        TimeSpan.FromSeconds(1), 
                        TimeSpan.FromSeconds(10), 
                        TimeSpan.FromSeconds(2)
                    ));

                    // Kết hợp Redelivery nếu lỗi kéo dài
                    e.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(5), 
                        TimeSpan.FromSeconds(15)
                    ));

                    e.ConfigureConsumer<ReleaseInventoryConsumer>(context);
                });
            });
        });
        // Đăng ký EF + repositories + stub IAuthenticatedUserService
    })
    .Build();

await host.RunAsync();