using MassTransit;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderAcceptService.Services
{
    public class OrderTimeoutJob
    {
        private readonly IBus _bus;

        public OrderTimeoutJob(IBus bus)
        {
            _bus = bus;
        }

        public async Task ExecuteAsync(Guid orderId, string targetAction)
        {
            await _bus.Publish(new OrderAutoTimeoutExpiredEvent(
                Guid.NewGuid(), 
                orderId, 
                targetAction, 
                DateTime.UtcNow));
        }
    }
}