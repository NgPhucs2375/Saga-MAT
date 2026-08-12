using Microsoft.EntityFrameworkCore;
using Onion.CleanArchitecture.Application.Features.Products.Queries.GetAllProducts;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Infrastructure.Persistence.Contexts;
using Onion.CleanArchitecture.Infrastructure.Persistence.Repository;
using Onion.CleanArchitecture.Infrastructure.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Infrastructure.Persistence.Repositories
{
    public class ProductRepositoryAsync : GenericRepositoryAsync<Product>, IProductRepositoryAsync
    {
        private readonly DbSet<Product> _products;
        private readonly DbContext _dbContext;

        public ProductRepositoryAsync(DbContext dbContext) : base(dbContext)
        {
            _products = dbContext.Set<Product>();
            _dbContext = dbContext;
        }

        public Task<bool> IsUniqueBarcodeAsync(string code)
        {
            return _products
                .AllAsync(p => p.Code != code);
        }

        public async Task<int> DeleteRangeAsync(List<int> ids)
        {
            var products = await _products.Where(p => ids.Contains(p.Id)).ToListAsync();
            _products.RemoveRange(products);
            return products.Count;
        }

        public async Task<PagedList<Product>> GetPagedProductsAsync(GetAllProductsParameter request)
        {
            var productQuery = _products.AsQueryable();
            if (request._filter != null && request._filter.Count > 0)
            {
                productQuery = MethodExtensions.ApplyFilters(productQuery, request._filter);
            }

            return await PagedList<Product>.ToPagedList(productQuery.OrderByDynamic(request._sort, request._order).AsNoTracking(), request._start, request._end);
        }

        public Task<Product> GetByProductIdAsync(Guid productId)
        {
            return _products.FirstOrDefaultAsync(p => p.ProductId == productId);
        }
            public async Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds)
        {
            return await _dbContext.Set<Product>()
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(Guid productId)
        {
            return await _products.AsTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public void MarkAsModified(Product entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
        }

        // ---- Giữ hàng (Reservation) với Optimistic Locking ----

        public async Task<bool> ReserveAsync(Guid productId, int quantity)
        {
            // UPDATE Product SET ReservedQty += @q, Version = Version + 1
            // WHERE ProductId = @id AND (PhysicalQty - ReservedQty) >= @q
            // Nếu 0 dòng -> hết hàng khả dụng (hoặc bị ai đó chiếm trước) -> fail ngay tại bước giữ chỗ.
            var updated = await _dbContext.Set<Product>()
                .Where(p => p.ProductId == productId && p.PhysicalQty - p.ReservedQty >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.ReservedQty, p => p.ReservedQty + quantity)
                    .SetProperty(p => p.Version, p => p.Version + 1));

            return updated > 0;
        }

        public async Task ReleaseAsync(Guid productId, int quantity)
        {
            await _dbContext.Set<Product>()
                .Where(p => p.ProductId == productId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.ReservedQty, p => p.ReservedQty - quantity)
                    .SetProperty(p => p.Version, p => p.Version + 1));
        }
    }
}
