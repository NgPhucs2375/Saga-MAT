using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using OrderAcceptService;
using OrderAcceptService.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // 1. Provider + stub user (phải trước AddNpgSqlPersistenceInfrastructure)
        services.AddTransient<IDatabaseSettingsProvider, DatabaseSettingsProvider>();
        services.AddScoped<IAuthenticatedUserService, SystemUserService>();

        // 2. DI ApplicationDbContext + Repositories + Shared
        services.AddNpgSqlPersistenceInfrastructure();
        services.AddPersistenceRepositories();
        services.AddSharedInfrastructure(ctx.Configuration);
                // 2. Cấu hình Connection String cho PostgreSQL Message Broker via Options Pattern
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = ctx.Configuration.GetConnectionString("PostgresConnection");
        });
        // 3. MassTransit: OrderAcceptConsumer + OrderTimeoutConsumer
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderAcceptConsumer>();
            x.AddConsumer<OrderTimeoutConsumer>();
            x.AddConsumer<CancelOrderConsumer>();
            x.AddSqlMessageScheduler();

            // BẮT BUỘC: đăng ký EF Outbox trên bus (thiếu => lỗi
            // "Instances of abstract classes cannot be created" ở OutboxConsumeFilter)
            x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
            {
                o.UsePostgres();
                // Tắt InboxCleanupService: tránh spam lỗi FK (InboxState bị xóa
                // khi OutboxMessage còn tham chiếu) ở phiên bản 8.3.0
                o.DisableInboxCleanupService();
            });

            x.UsingPostgres((context, cfg) =>
            {
                // Tự động khởi tạo schema/bảng queue trong PostgreSQL nếu chưa có
                cfg.AutoStart = true;

                // Nhận các command/event theo tên queue cố định
                cfg.ReceiveEndpoint("order-accept-queue", e =>
                {
                    e.ConfigureConsumer<OrderAcceptConsumer>(context);
                } );

                cfg.ReceiveEndpoint("order-cancel-queue", e =>{ 
                    e.ConfigureConsumer<CancelOrderConsumer>(context);});

                cfg.ReceiveEndpoint("order-timeout-queue", e => {
                    e.ConfigureConsumer<OrderTimeoutConsumer>(context);
                });

                cfg.UseSqlMessageScheduler();

            });
        });
    })
    .Build();

await host.RunAsync();
