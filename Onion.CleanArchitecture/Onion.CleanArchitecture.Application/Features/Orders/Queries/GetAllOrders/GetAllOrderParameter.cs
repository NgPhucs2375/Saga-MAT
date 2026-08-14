using Onion.CleanArchitecture.Application.Filters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders
{
    public class GetAllOrdersParameter : RequestParameter
    {
        public string CustomerId { get; set; }
    }
}
