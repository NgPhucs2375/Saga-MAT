using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Infrastructure.Identity.Contexts;
using Onion.CleanArchitecture.Infrastructure.Identity.Models;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

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
                await dbContext.Database.MigrateAsync();
                var identityDbContext = _serviceProvider.GetRequiredService<IdentityContext>();
                await identityDbContext.Database.MigrateAsync();

                // Backfill: gán ProductId duy nhất cho các sản phẩm cũ đang bị Guid.Empty (trùng key, gây lỗi khi tạo đơn)
                var orphanProducts = await dbContext.Product.Where(p => p.ProductId == Guid.Empty).ToListAsync();
                foreach (var product in orphanProducts)
                {
                    product.ProductId = Guid.NewGuid();
                }
                if (orphanProducts.Count > 0)
                {
                    await dbContext.SaveChangesAsync();
                    Log.Information("Đã backfill {Count} sản phẩm có ProductId rỗng", orphanProducts.Count);
                }

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
