using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using OrderOrchestration;
using OrderOrchestration.Activities;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        // 1. Saga DbContext (PostgreSQL) - lưu saga instance để resume sau crash
        services.AddDbContext<OrderSagaDbContext>(options =>
            options.UseNpgsql(ctx.Configuration.GetConnectionString("PostgresConnection")));

        // 2. Cấu hình Connection String cho PostgreSQL Message Broker via Options Pattern
        services.Configure<SqlTransportOptions>(options =>
        {
            options.ConnectionString = ctx.Configuration.GetConnectionString("PostgresConnection");
        });



        // 3. Đăng ký SagaStateMachine với repository EF Core (PostgreSQL) + transport PostgreSQL
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();
            // Tự động scan và đăng ký toàn bộ Activities vào DI Container với đúng Scope
            x.AddActivities(typeof(OrderCreatedActivity).Assembly);

            // Cấu hình EF Core Outbox gắn liền với OrderSagaDbContext
            x.AddEntityFrameworkOutbox<OrderSagaDbContext>(o =>
            {
                o.UsePostgres(); // Sử dụng PostgreSQL Outbox
                o.UseBusOutbox(); // Bật Outbox cho Bus (Gửi Command/Saga từ Saga)
                o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30); // Thời gian phát hiện trùng lặp (Duplicate Detection) cho Outbox
            });
            // Saga State Machine 
            x.AddSagaStateMachine<OrderSagaStateMachine, OrderState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<OrderSagaDbContext>();
                    r.UsePostgres();
                });

            x.UsingPostgres((context, cfg) =>
            {
                cfg.AutoStart = true;

                // Saga receive endpoint + outbox được cấu hình tự động từ bus-level
                // AddEntityFrameworkOutbox + UseBusOutbox (tránh double-tracking Outbox).
                cfg.ConfigureEndpoints(context);
            });
        });
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderSagaDbContext>();
    db.Database.Migrate();
    Console.WriteLine("[OrderOrchestration] Đã migrate OrderSagaDbContext (thêm cột StepsCompleted).");
}

await host.RunAsync();
