using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using ApproveOrderService;
using ApproveOrderService.Consumers;
using ApproveOrderService.Services;

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
            x.AddConsumer<ApproveOrderConsumer>();
            x.AddConsumer<RejectOrderConsumer>();
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

                cfg.ReceiveEndpoint("order-approve-queue", e => {
                    e.ConfigureConsumer<ApproveOrderConsumer>(context);
                });

                cfg.ReceiveEndpoint("order-reject-queue", e => {
                    e.ConfigureConsumer<RejectOrderConsumer>(context);
                });

                cfg.UseSqlMessageScheduler();
            });
        });

    })
    .Build();

await host.RunAsync();
