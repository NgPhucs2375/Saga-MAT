using AutoMapper;
using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Products.Commands.CreateProduct
{
    public partial class CreateProductCommand : IRequest<Response<int>>
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public decimal Price { get; set; }
        public int PhysicalQty { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Response<int>>
    {
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IMapper _mapper;
        public CreateProductCommandHandler(IProductRepositoryAsync productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<Response<int>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var product = _mapper.Map<Product>(request);
            await _productRepository.AddAsync(product);
            return new Response<int>(product.Id);
        }
    }
}
