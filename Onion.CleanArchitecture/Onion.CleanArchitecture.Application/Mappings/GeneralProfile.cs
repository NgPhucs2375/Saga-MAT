using AutoMapper;
using Onion.CleanArchitecture.Application.Features.Orders.Queries.GetAllOrders;
using Onion.CleanArchitecture.Application.Features.Products.Commands.CreateProduct;
using Onion.CleanArchitecture.Application.Features.Products.Queries.GetAllProducts;
using Onion.CleanArchitecture.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.CleanArchitecture.Application.Mappings
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            CreateMap<Product, GetAllProductsViewModel>()
                .ForMember(dest => dest.SLTKho, opt => opt.MapFrom(src => src.PhysicalQty))
                .ReverseMap();
            CreateMap<CreateProductCommand, Product>();
            CreateMap<GetAllProductsQuery, GetAllProductsParameter>();
            CreateMap<GetAllOrderQuery, GetAllOrdersParameter>();
        }
    }
}
