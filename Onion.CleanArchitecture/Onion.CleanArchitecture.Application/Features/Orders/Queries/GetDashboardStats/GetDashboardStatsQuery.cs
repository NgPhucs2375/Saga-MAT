using MediatR;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Application.Wrappers;
using Onion.CleanArchitecture.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Application.Features.Dashboard.Queries.GetDashboardStats
{
    public class DashboardStatsViewModel
    {
        public int TotalOrders { get; set; }
        public int SubmittedCount { get; set; }
        public int AcceptedCount { get; set; }
        public int CompletedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class GetDashboardStatsQuery : IRequest<Response<DashboardStatsViewModel>>
    {
    }

    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, Response<DashboardStatsViewModel>>
    {
        private readonly IOrderRepositoryAsync _orderRepository;

        public GetDashboardStatsQueryHandler(IOrderRepositoryAsync orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Response<DashboardStatsViewModel>> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
        {
            var submitted = await _orderRepository.CountByStatusAsync(OrderStatus.Submitted);
            var accepted = await _orderRepository.CountByStatusAsync(OrderStatus.Accepted);
            var completed = await _orderRepository.CountByStatusAsync(OrderStatus.Completed);
            var rejected = await _orderRepository.CountByStatusAsync(OrderStatus.Rejected);

            var stats = new DashboardStatsViewModel
            {
                TotalOrders = submitted + accepted + completed + rejected,
                SubmittedCount = submitted,
                AcceptedCount = accepted,
                CompletedCount = completed,
                RejectedCount = rejected,
                TotalRevenue = await _orderRepository.SumTotalAmountByStatusAsync(OrderStatus.Completed)
            };

            return new Response<DashboardStatsViewModel>(stats);
        }
    }
}
