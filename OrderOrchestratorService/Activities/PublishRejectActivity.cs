    using MassTransit;
using Microsoft.Extensions.Logging;
using Onion.CleanArchitecture.Application.Interfaces.Repositories;
using Onion.CleanArchitecture.Domain.Entities;
    using Onion.CleanArchitecture.Domain.Events;
    using System.Text.Json;
    using System.Threading.Tasks;
namespace OrderOrchestratorService.Activities
{
    public class PublishRejectActivity : IStateMachineActivity<OrderState, OrderValidationFailedEvent>
    {
        private readonly ILogger<PublishRejectActivity> _logger;
        private readonly IOrderRepositoryAsync _orderRepository;
        
        public void Accept(StateMachineVisitor visitor)

        {
            throw new NotImplementedException();
        }

        public Task Execute(BehaviorContext<OrderState, OrderValidationFailedEvent> context, IBehavior<OrderState, OrderValidationFailedEvent> next)
        {
            throw new NotImplementedException();
        }

        public Task Faulted<TException>(BehaviorExceptionContext<OrderState, OrderValidationFailedEvent, TException> context, IBehavior<OrderState, OrderValidationFailedEvent> next) where TException : Exception
        {
            throw new NotImplementedException();
        }

        public void Probe(ProbeContext context)
        {
            throw new NotImplementedException();
        }
    }
}


