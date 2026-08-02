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
        public int SLTKho { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
