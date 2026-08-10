using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Domain.Entities;
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
        {            //Read Configuration from appSettings
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
                await EnsureMassTransitOutboxSchemaAsync(dbContext);
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

                // Seed sample products if none exist
                await SeedSampleProductsAsync(dbContext);

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

        private async Task SeedSampleProductsAsync(ApplicationDbContext dbContext)
        {
            var productCount = await dbContext.Product.CountAsync();
            if (productCount > 0) return;

            var sampleProducts = new List<Product>
            {
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP001",
                    Name = "Áo thun nam cotton 100%",
                    Description = "Áo thun form rộng, chất liệu cotton 100% thoáng mát",
                    Rate = 0,
                    Price = 199000,
                    PhysicalQty = 100,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP002",
                    Name = "Quần jean nam slimfit",
                    Description = "Quần jean co giãn, form slimfit hiện đại",
                    Rate = 0,
                    Price = 450000,
                    PhysicalQty = 50,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP003",
                    Name = "Áo khoác bomber unisex",
                    Description = "Áo khoác bomber form oversize, phù hợp mọi giới",
                    Rate = 0,
                    Price = 350000,
                    PhysicalQty = 30,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP004",
                    Name = "Váy hoa'été nữ",
                    Description = "Váy maxi hoa nhẹ, chất voan mỏng mát",
                    Rate = 0,
                    Price = 280000,
                    PhysicalQty = 40,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP005",
                    Name = "Sneaker trắng unisex",
                    Description = "Giày sneaker da PU, đế cao su bền bỉ",
                    Rate = 0,
                    Price = 550000,
                    PhysicalQty = 25,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP006",
                    Name = "Túi tote canvas",
                    Description = "Túi xỏ canvas dày dặn, in logo tùy chỉnh",
                    Rate = 0,
                    Price = 120000,
                    PhysicalQty = 60,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP007",
                    Name = "Mũ len beanie",
                    Description = "Mũ len dày dặn, nhiều màu sắc",
                    Rate = 0,
                    Price = 85000,
                    PhysicalQty = 80,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                },
                new Product
                {
                    ProductId = Guid.NewGuid(),
                    Code = "SP008",
                    Name = "Đồng hồ cơ nam",
                    Description = "Đồng hồ cơ tự động, dây da thật",
                    Rate = 0,
                    Price = 2500000,
                    PhysicalQty = 10,
                    ReservedQty = 0,
                    IsActive = true,
                    Version = 1
                }
            };

            await dbContext.Product.AddRangeAsync(sampleProducts);
            await dbContext.SaveChangesAsync();
            Log.Information("Đã seed {Count} sản phẩm mẫu", sampleProducts.Count);
        }

        // Tạo bảng Outbox/Inbox của MassTransit (idempotent) cho ApplicationDbContext.
        // Các worker (Submit/Accept/Complete/Saga) dùng UseEntityFrameworkOutbox<ApplicationDbContext>
        // trên CÙNG database/schema "public", nên chỉ cần tạo 1 lần ở đây là đủ cho tất cả.
        private async Task EnsureMassTransitOutboxSchemaAsync(ApplicationDbContext dbContext)
        {
            var statements = new[]
            {
                """CREATE TABLE IF NOT EXISTS "InboxState" ("Id" bigint GENERATED BY DEFAULT AS IDENTITY, "MessageId" uuid NOT NULL, "ConsumerId" uuid NOT NULL, "LockId" uuid NOT NULL, "RowVersion" bytea NULL, "Received" timestamp with time zone NOT NULL, "ReceiveCount" integer NOT NULL, "ExpirationTime" timestamp with time zone NULL, "Consumed" timestamp with time zone NULL, "Delivered" timestamp with time zone NULL, "LastSequenceNumber" bigint NULL, CONSTRAINT "PK_InboxState" PRIMARY KEY ("Id"), CONSTRAINT "AK_InboxState_MessageId_ConsumerId" UNIQUE ("MessageId", "ConsumerId"))""",
                """CREATE INDEX IF NOT EXISTS "IX_InboxState_Delivered" ON "InboxState" ("Delivered")""",
                """CREATE TABLE IF NOT EXISTS "OutboxState" ("OutboxId" uuid NOT NULL, "LockId" uuid NOT NULL, "RowVersion" bytea NULL, "Created" timestamp with time zone NOT NULL, "Delivered" timestamp with time zone NULL, "LastSequenceNumber" bigint NULL, CONSTRAINT "PK_OutboxState" PRIMARY KEY ("OutboxId"))""",
                """CREATE INDEX IF NOT EXISTS "IX_OutboxState_Created" ON "OutboxState" ("Created")""",
                """CREATE TABLE IF NOT EXISTS "OutboxMessage" ("SequenceNumber" bigint GENERATED BY DEFAULT AS IDENTITY, "EnqueueTime" timestamp with time zone NULL, "SentTime" timestamp with time zone NOT NULL, "Headers" text NULL, "Properties" text NULL, "InboxMessageId" uuid NULL, "InboxConsumerId" uuid NULL, "OutboxId" uuid NULL, "MessageId" uuid NOT NULL, "ContentType" character varying(256) NOT NULL, "MessageType" text NOT NULL, "Body" text NOT NULL, "ConversationId" uuid NULL, "CorrelationId" uuid NULL, "InitiatorId" uuid NULL, "RequestId" uuid NULL, "SourceAddress" character varying(256) NULL, "DestinationAddress" character varying(256) NULL, "ResponseAddress" character varying(256) NULL, "FaultAddress" character varying(256) NULL, "ExpirationTime" timestamp with time zone NULL, CONSTRAINT "PK_OutboxMessage" PRIMARY KEY ("SequenceNumber"), CONSTRAINT "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId" FOREIGN KEY ("InboxMessageId", "InboxConsumerId") REFERENCES "InboxState" ("MessageId", "ConsumerId"), CONSTRAINT "FK_OutboxMessage_OutboxState_OutboxId" FOREIGN KEY ("OutboxId") REFERENCES "OutboxState" ("OutboxId"))""",
                """CREATE INDEX IF NOT EXISTS "IX_OutboxMessage_EnqueueTime" ON "OutboxMessage" ("EnqueueTime")""",
                """CREATE INDEX IF NOT EXISTS "IX_OutboxMessage_ExpirationTime" ON "OutboxMessage" ("ExpirationTime")""",
                """CREATE UNIQUE INDEX IF NOT EXISTS "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber" ON "OutboxMessage" ("InboxMessageId", "InboxConsumerId", "SequenceNumber")""",
                """CREATE UNIQUE INDEX IF NOT EXISTS "IX_OutboxMessage_OutboxId_SequenceNumber" ON "OutboxMessage" ("OutboxId", "SequenceNumber")""",
            };

            foreach (var sql in statements)
            {
                await dbContext.Database.ExecuteSqlRawAsync(sql);
            }

            Log.Information("Đã đảm bảo bảng MassTransit Outbox (InboxState/OutboxState/OutboxMessage) tồn tại.");
        }
    }
}
