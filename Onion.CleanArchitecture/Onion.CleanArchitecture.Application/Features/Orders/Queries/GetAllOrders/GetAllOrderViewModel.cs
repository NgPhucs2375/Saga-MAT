using System;
using System.Collections.Generic;
using System.Text;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Enums;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders
{
    public class GetAllOrderViewModel
    {
        public int Id { get; set; }
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; }
        public OrderStatus Status { get; set; }
        public string CustomerId { get; set; }
        public string ShippingAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<OrderItem> OrderItems;
    }}