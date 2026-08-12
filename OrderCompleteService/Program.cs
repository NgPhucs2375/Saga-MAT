using MassTransit;
using Microsoft.EntityFrameworkCore;
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
using OrderCompleteService.Context;
using OrderCompleteService.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        //1. Đăng ký Provider trước khi AddNpgSqlPersistenceInfrastructure
        services.AddScoped<IAuthenticatedUserService, SystemUserService>();
        services.AddSharedInfrastructure(ctx.Configuration);
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = ctx.Configuration.GetConnectionString("BrokerConnection");
        });

        services.AddDbContext<FulfillmentDbContext>(o =>
            o.UseNpgsql(ctx.Configuration.GetConnectionString("BusinessConnection")));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<OrderCompleteConsumer>();
            x.AddConsumer<ReleaseInventoryConsumer>();
            x.SetKebabCaseEndpointNameFormatter();

            x.AddEntityFrameworkOutbox<FulfillmentDbContext>(o =>
            {
                o.UsePostgres(); // Khai báo dùng PostgreSQL provider
                o.UseBusOutbox();
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
                {   e.ConfigureConsumer<OrderCompleteConsumer>(context); });

                cfg.ReceiveEndpoint("release-inventory-queue", e => {
                    e.ConfigureConsumer<ReleaseInventoryConsumer>(context);
                });
            });
        });
        // Đăng ký EF + repositories + stub IAuthenticatedUserService
    })
    .Build();

await host.RunAsync();