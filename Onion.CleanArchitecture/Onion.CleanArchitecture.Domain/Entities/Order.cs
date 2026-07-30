using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class Order : AuditableBaseEntity
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public string CustomerId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public string ShippingAddress { get; set; }
        public string Note { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? RejectedAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}