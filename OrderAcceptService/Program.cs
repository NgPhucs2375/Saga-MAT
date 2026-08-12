using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using OrderAcceptService;
using OrderAcceptService.Context;
using OrderAcceptService.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddScoped<IAuthenticatedUserService, SystemUserService>();

        services.AddSharedInfrastructure(ctx.Configuration);
        // 2. Cấu hình Connection String cho PostgreSQL Message Broker via Options Pattern
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = ctx.Configuration.GetConnectionString("BrokerConnection");
        });

        // 
        services.AddDbContext<ValidationDbContext>(o =>
            o.UseNpgsql(ctx.Configuration.GetConnectionString("BusinessConnection")));
        // 3. MassTransit: OrderAcceptConsumer + OrderTimeoutConsumer
        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderAcceptConsumer>();
            x.AddConsumer<OrderTimeoutConsumer>();
            x.AddConsumer<OrderCompleteFailedConsumer>();
            x.AddConsumer<CancelOrderConsumer>();

            x.AddEntityFrameworkOutbox<ValidationDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
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
                
                cfg.ReceiveEndpoint("order-complete-failed-queue", e => {
                    e.ConfigureConsumer<OrderCompleteFailedConsumer>(context);
                });


            });
        });

        // 4. Worker quét timer hết hạn
        services.AddHostedService<TimerWatcherBackgroundService>();
    })
    .Build();

await host.RunAsync();
