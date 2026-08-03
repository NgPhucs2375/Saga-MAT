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

        services.AddScoped<OrderCreatedActivity>();
        services.AddScoped<OrderValidatedActivity>();
        services.AddScoped<OrderValidationFailedActivity>();
        services.AddScoped<OrderAcceptedActivity>();
        services.AddScoped<OrderAcceptFailedActivity>();
        services.AddScoped<OrderCompletedActivity>();
        services.AddScoped<OrderCompleteFailedActivity>();
        services.AddScoped<OrderTimeoutExpiredActivity>();

        // 3. Đăng ký SagaStateMachine với repository EF Core (PostgreSQL) + transport PostgreSQL
        services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            x.AddSagaStateMachine<OrderSagaStateMachine, OrderState>()
                .EntityFrameworkRepository(r =>
                {
                    r.ExistingDbContext<OrderSagaDbContext>();
                    r.UsePostgres();
                });

            x.UsingPostgres((context, cfg) =>
            {
                cfg.AutoStart = true;
                cfg.ConfigureEndpoints(context);
            });
        });
    })
    .Build();

await host.RunAsync();
