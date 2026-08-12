using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmsService.Consumer;
using SmsService.Services;
using SMSService.Context;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;
using DotNetEnv;
using Npgsql.EntityFrameworkCore.PostgreSQL;

Env.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();
// Đăng ký DI cho SMS Provider
builder.Services.AddHttpClient<ISmsProviderService, SmsProviderService>();

// 1. Provider + stub user
builder.Services.AddTransient<IDatabaseSettingsProvider, DatabaseSettingsProvider>();
builder.Services.AddScoped<IAuthenticatedUserService, SystemUserService>();

// 2. DI SmsDbContext (riêng cho SMSService) + Shared (đăng ký IDateTimeService)
builder.Services.AddDbContext<SmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("BusinessConnection")));
builder.Services.AddSharedInfrastructure(builder.Configuration);

// 3. Cấu hình Connection String cho PostgreSQL Broker (Message Broker) qua Options Pattern
builder.Services.Configure<SqlTransportOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("BrokerConnection");
});

// 4. Cấu hình MassTransit
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<SmsConsumer>();

    // Đăng ký Outbox cho SmsDbContext
    x.AddEntityFrameworkOutbox<SmsDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(30);
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

// Tự động migrate SmsDbContext khi start
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmsDbContext>();
    db.Database.Migrate();
}

await host.RunAsync();