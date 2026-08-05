using MediatR;
using Onion.CleanArchitecture.Application.Exceptions;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Products.Commands.UpdateProduct
{
    public class UpdateProductCommand : IRequest<Response<int>>
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public int SLTKho { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Response<int>>
        {
            private readonly IProductRepositoryAsync _productRepository;
            public UpdateProductCommandHandler(IProductRepositoryAsync productRepository)
            {
                _productRepository = productRepository;
            }
            public async Task<Response<int>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
            {
                var product = await _productRepository.GetByIdAsync(command.Id);

                if (product == null)
                {
                    throw new ApiException($"Product Not Found.");
                }
                else
                {
                    product.Code = command.Code;
                    product.Name = command.Name;
                    product.Rate = command.Rate;
                    product.Description = command.Description;
                    product.PhysicalQty = command.SLTKho;
                    product.Price = command.Price;
                    product.IsActive = command.IsActive;
                    await _productRepository.UpdateAsync(product);
                    return new Response<int>(product.Id);
                }
            }
        }
    }
}
