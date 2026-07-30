using Onion.CleanArchitecture.Application;
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
_services.AddNpgSqlPersistenceInfrastructure();
_services.AddNpgSqlPersistenceInfrastructureIdentity();
_services.AddIdentityRepositories(_config);
_services.AddSqlServerPersistenceInfrastructure(typeof(Program).Assembly.FullName);
_services.AddPersistenceRepositories();
_services.AddSharedInfrastructure(_config);
if (_env.IsDevelopment())
{
    _services.AddSwaggerExtension();
}

_services.AddControllers().AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);
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
app.UseAuthorization();

app.UseErrorHandlingMiddleware();
app.UseHealthChecks("/health");
app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
