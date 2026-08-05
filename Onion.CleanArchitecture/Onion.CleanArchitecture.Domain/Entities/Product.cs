using Onion.CleanArchitecture.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class Product : AuditableBaseEntity
    {
        public Guid ProductId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public string Description { get; set; }
        // Tồn kho vật lý thực tế trong kho (map sang cột "SLTKho" cũ)
        public int PhysicalQty { get; set; }
        // Tồn kho đang tạm giữ (khách đã đặt/chấp nhận nhưng chưa hoàn tất)
        public int ReservedQty { get; set; }
        // Số lượng khả dụng để bán = PhysicalQty - ReservedQty
        public int AvailableQty => PhysicalQty - ReservedQty;
        // Concurrency token (Optimistic Locking): tăng lên mỗi khi update
        public int Version { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
