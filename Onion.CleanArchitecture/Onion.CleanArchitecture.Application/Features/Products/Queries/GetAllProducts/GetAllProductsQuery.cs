using AutoMapper;
using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Onion.CleanArchitecture.Application.Features.Products.Queries.GetAllProducts
{
    public class GetAllProductsQuery : IRequest<Response<object>>
    {
        public int _start { get; set; }
        public int _end { get; set; }
        public string _sort { get; set; }
        public string _order { get; set; }
        public List<string> _filter { get; set; }
    }
    public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Response<object>>
    {
        private readonly IProductRepositoryAsync _productRepository;
        private readonly IMapper _mapper;
        public GetAllProductsQueryHandler(IProductRepositoryAsync productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<Response<object>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
        {
            var validFilter = _mapper.Map<GetAllProductsParameter>(request);
            var pagedProducts = await _productRepository.GetPagedProductsAsync(validFilter);
            
            // Map entities to ViewModel to include SLTKho
            var viewModels = _mapper.Map<List<GetAllProductsViewModel>>(pagedProducts);
            
            return new Response<object>(true, new
            {
                pagedProducts._start,
                pagedProducts._end,
                pagedProducts._total,
                pagedProducts._hasNext,
                pagedProducts._hasPrevious,
                pagedProducts._pages,
                _data = viewModels
            }, message: "Success");
        }
    }
}
