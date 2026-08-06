using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using OrderSubmitService;
using OrderSubmitService.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // 1. Đăng ký các Service hạ tầng & DI
        services.AddTransient<IDatabaseSettingsProvider, DatabaseSettingsProvider>();
        services.AddScoped<IAuthenticatedUserService, SystemUserService>();
        services.AddNpgSqlPersistenceInfrastructure();
        services.AddPersistenceRepositories();
        services.AddSharedInfrastructure(ctx.Configuration);

        // 2. Cấu hình Connection String cho PostgreSQL Message Broker via Options Pattern
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = ctx.Configuration.GetConnectionString("PostgresConnection");
        });

        // 3. Đăng ký MassTransit duy nhất 1 lần với Postgres Transport
        services.AddMassTransit(x =>
        {
            // Định dạng tên Queue theo chuẩn kebab-case (ví dụ: order-submit-consumer)
            x.SetKebabCaseEndpointNameFormatter();

// Đăng ký Consumer xử lý ValidateOrderCommand
            x.AddConsumer<OrderSubmitConsumer>();

            x.UsingPostgres((context, cfg) =>
            {
                // Tự động khởi tạo schema/bảng queue trong PostgreSQL nếu chưa có
                cfg.AutoStart = true;

                // BẮT BUỘC: Nhận ValidateOrderCommand từ Saga qua queue order-validation-queue
                cfg.ReceiveEndpoint("order-validation-queue", e =>
                {
                    e.UseEntityFrameworkOutbox<ApplicationDbContext>(context);
                    e.ConfigureConsumer<OrderSubmitConsumer>(context);
                });
            });
        });
    })
    .Build();

await host.RunAsync();