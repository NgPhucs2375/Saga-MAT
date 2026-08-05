using Onion.CleanArchitecture.Application.Features.Products.Queries.GetAllProducts;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Interfaces.Repositories
{
    public interface IProductRepositoryAsync : IGenericRepositoryAsync<Product>
    {
        Task<bool> IsUniqueBarcodeAsync(string barcode);
        Task<int> DeleteRangeAsync(List<int> ids);
         Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds);
        Task<PagedList<Product>> GetPagedProductsAsync(GetAllProductsParameter parameter);
        Task<Onion.CleanArchitecture.Domain.Entities.Product> GetProductByIdAsync(Guid productId);
        void MarkAsModified(Onion.CleanArchitecture.Domain.Entities.Product entity);
        /// <summary>
        /// Tăng ReservedQty (giữ hàng) cho sản phẩm nếu còn đủ hàng khả dụng.
        /// Atomic + Optimistic Locking: trả false nếu hết hàng / bị người khác chiếm.
        /// </summary>
        Task<bool> ReserveAsync(Guid productId, int quantity);
        /// <summary>
        /// Giảm ReservedQty (hoàn lại hàng đang giữ khi compensate/timeout/cancel).
        /// </summary>
        Task ReleaseAsync(Guid productId, int quantity);
    }
}
