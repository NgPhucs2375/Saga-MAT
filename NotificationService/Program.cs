using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Consumers;
using NotificationService.Hubs;
using NotificationService.Services;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.Infrastructure.Shared.Environments;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

// === DI SignalR + CORS === //
// Giữ nguyên PascalCase khi serialize qua wire để khớp client TS (NotificationPayload)
builder.Services.AddSignalR().AddJsonProtocol(o =>
{
    o.PayloadSerializerOptions.PropertyNamingPolicy = null;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpa", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                allowedOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// === DI EF + Repositories + Shared === //
builder.Services.AddTransient<IDatabaseSettingsProvider, DatabaseSettingsProvider>();
builder.Services.AddScoped<IAuthenticatedUserService, SystemUserService>();

var postgresConn = builder.Configuration.GetConnectionString("PostgresConnection");
if (!string.IsNullOrWhiteSpace(postgresConn))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            postgresConn,
            b =>
            {
                b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            }));
}
builder.Services.AddPersistenceRepositories();
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.Configure<SqlTransportOptions>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("PostgresConnection");
});

// === DI Notification Dispatcher === //
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

// === MassTransit: consume các Response event === //
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmitSuccessConsumer>();
    x.AddConsumer<OrderSubmitFailedConsumer>();
    x.AddConsumer<OrderAcceptSuccessConsumer>();
    x.AddConsumer<OrderAcceptFailedConsumer>();
    x.AddConsumer<OrderCompleteSuccessConsumer>();
    x.AddConsumer<OrderCompleteFailedConsumer>();
    x.UsingPostgres((context, cfg) =>
    {
        cfg.AutoStart = true;
        cfg.ReceiveEndpoint("notification-queue", e =>
        {
            e.ConfigureConsumer<OrderSubmitSuccessConsumer>(context);
            e.ConfigureConsumer<OrderSubmitFailedConsumer>(context);
            e.ConfigureConsumer<OrderAcceptSuccessConsumer>(context);
            e.ConfigureConsumer<OrderAcceptFailedConsumer>(context);
            e.ConfigureConsumer<OrderCompleteSuccessConsumer>(context);
            e.ConfigureConsumer<OrderCompleteFailedConsumer>(context);
        });
    });
});

var app = builder.Build();

app.UseCors("AllowSpa");
app.UseRouting();

app.MapGet("/", () => "NotificationService - SignalR Hub: /hubs/notification");
app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
