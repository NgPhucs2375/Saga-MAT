using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Infrastructure.Identity.Contexts;
using Onion.CleanArchitecture.Infrastructure.Identity.Models;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Serilog;

namespace Onion.CleanArchitecture.WebApp.Server.Initializer
{
    public class ApplicationInitializer
    {
        private readonly IServiceProvider _serviceProvider;

        public ApplicationInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            //Read Configuration from appSettings
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
            try
            {
                var dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
                var identityDbContext = _serviceProvider.GetRequiredService<IdentityContext>();
                identityDbContext.Database.EnsureCreated();

                var userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                await Infrastructure.Identity.Seeds.DefaultRoles.SeedAsync(userManager, roleManager);
                await Infrastructure.Identity.Seeds.DefaultSuperAdmin.SeedAsync(userManager, roleManager);
                await Infrastructure.Identity.Seeds.DefaultBasicUser.SeedAsync(userManager, roleManager);
                Log.Information("Hoàn thành khởi tạo dữ liệu mặc định");
                Log.Information("BẮT ĐẦU KHỞI TẠO DỮ LIỆU MẪU");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Lỗi khi khởi tạo dữ liệu mặc định");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
