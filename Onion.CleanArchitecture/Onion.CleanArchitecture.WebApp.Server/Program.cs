using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Onion.CleanArchitecture.Application;
using Onion.CleanArchitecture.Application.Hubs;
using Onion.CleanArchitecture.Application.Interfaces;
using Onion.CleanArchitecture.Infrastructure.Identity;
using Onion.CleanArchitecture.Infrastructure.Persistence;
using Onion.CleanArchitecture.Infrastructure.Shared;
using Onion.CleanArchitecture.WebApp.Server.Extensions;
using Onion.CleanArchitecture.WebApp.Server.Initializer;
using Onion.CleanArchitecture.WebApp.Server.Services;
var builder = WebApplication.CreateBuilder(args);
var _config = builder.Configuration;
var _services = builder.Services;
var _env = builder.Environment;
// Add services to the container.

_services.AddEnvironmentVariablesExtension();
_services.AddIdentityLayer();
_services.AddApplicationLayer();
_services.AddNpgSqlPersistenceInfrastructureIdentity();
_services.AddIdentityRepositories(_config);
_services.AddSharedInfrastructure(_config);

// SignalR dùng PascalCase để khớp type NotificationPayload trên Client
// (mặc định SignalR serialize camelCase -> không khớp key FE đang đọc).
_services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

_services.Configure<SqlTransportOptions>(options =>
{
    options.ConnectionString = _config.GetConnectionString("BrokerConnection");
});



// Đăng ký MassTransit để WebApp có thể publish events (kế tạo Saga)
_services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // Đăng ký consumer nhận các Response event từ Saga -> đẩy Notification tới UI qua SignalR
    x.AddConsumer<OrderNotificationConsumer>();

    x.UsingPostgres((context, cfg) =>
    {
        // Tự động khởi tạo cấu trúc bảng queue/transport nếu chưa có
        cfg.AutoStart = true;
        // Auto-tạo receive endpoint --> OrderNotificationConsumer lắng nghe response từ Saga
        cfg.ConfigureEndpoints(context);
    });
});

if (_env.IsDevelopment())
{
    _services.AddSwaggerExtension();
    // 1 lenh `dotnet run` -> tu dong spawn cac microservice con lai (Saga + Notification)
    _services.AddHostedService<MicroserviceLauncherHostedService>();
}

_services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.PropertyNamingPolicy = null;
    // Tránh circular reference (Order -> OrderItems -> Order) khi serialize entity
    opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
_services.AddApiVersioningExtension();
_services.AddHealthChecks();
_services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
_services.AddEndpointsApiExplorer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = new ApplicationInitializer(scope.ServiceProvider);
    await initializer.InitializeAsync();
}

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (_env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwaggerExtension();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.UseErrorHandlingMiddleware();
app.UseHealthChecks("/health");
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");

app.MapFallbackToFile("/index.html");

app.Run();
