using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmsService.Consumer;
using SmsService.Services;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using DotNetEnv;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;

Env.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
// Đăng ký DI cho SMS Provider
builder.Services.AddHttpClient<ISmsProviderService, SmsProviderService>();

// 1. Provider + stub user (phải trước AddNpgSqlPersistenceInfrastructure)
builder.Services.AddTransient<IDatabaseSettingsProvider, DatabaseSettingsProvider>();
builder.Services.AddScoped<IAuthenticatedUserService, SystemUserService>();

// 1b. DI ApplicationDbContext + Repositories + Shared (đăng ký IDateTimeService)
builder.Services.AddNpgSqlPersistenceInfrastructure();
builder.Services.AddPersistenceRepositories();
builder.Services.AddSharedInfrastructure(builder.Configuration);

// 1c. Cấu hình Connection String cho PostgreSQL Transaction (Message Broker) qua Options Pattern
builder.Services.Configure<SqlTransportOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("PostgresConnection");
});

// 2. Cấu hình MassTransit
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<SmsConsumer>();

    // Đăng ký Inbox cho SmsDbContext
    x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
    {
        o.UsePostgres();
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
        // Tắt InboxCleanupService: tránh spam lỗi FK (InboxState bị xóa
        // khi OutboxMessage còn tham chiếu) ở phiên bản 8.3.0
        o.DisableInboxCleanupService();
    });

    x.UsingPostgres((context, cfg) =>
    {
        cfg.AutoStart = true;

        cfg.ReceiveEndpoint("sms-service-queue", e =>
        {
            // Cấu hình Thử lại (Retry) 3 lần, mỗi lần cách nhau 5 giây nếu gọi API Provider bị lỗi ngắt kết nối
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));

            e.ConfigureConsumer<SmsConsumer>(context);
        });
    });
});

var host = builder.Build();
await host.RunAsync();