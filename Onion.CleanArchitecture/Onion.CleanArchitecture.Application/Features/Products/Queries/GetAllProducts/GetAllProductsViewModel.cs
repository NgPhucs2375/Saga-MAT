using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.CleanArchitecture.Application.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsViewModel
    {
        public int Id { get; set; }
        public Guid ProductId { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public decimal Price { get; set; }
        // Tồn kho vật lý (map cột "SLTKho")
        public int PhysicalQty { get; set; }
        // Tồn kho đang tạm giữ
        public int ReservedQty { get; set; }
        // Số lượng khả dụng để bán = PhysicalQty - ReservedQty
        public int AvailableQty => PhysicalQty - ReservedQty;
    }
}
