using MassTransit;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Events;
using System.Text.Json;

namespace OrderOrchestratorService.Activities
{
    public class ValidateOrderActivity : ISagaActivity<OrderState, OrderCreatedEvent>
    {
        public async Task Execute(
            BehaviorContext<OrderState, OrderCreatedEvent> context,
            IPipe<BehaviorContext<OrderState, OrderCreatedEvent>> next)
        {
            var msg = context.Message;
            context.Saga.CustomerId = msg.CustomerId.ToString();
            context.Saga.TotalAmount = msg.TotalAmount;
            context.Saga.Items = JsonSerializer.Serialize(msg.Items);
            context.Saga.CreatedAt = msg.Timestamp;

            await context.Send(new Uri("queue:order-submit-service"),
                new ValidateOrderCommand(msg.EventId, msg.OrderId, msg.CustomerId, msg.Items, msg.Timestamp));

            await next.Execute(context);
        }

        public async Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderCreatedEvent, TException> context,
            IPipe<BehaviorExceptionContext<OrderState, OrderCreatedEvent, TException>> next)
            where TException : Exception
        {
            await next.Faulted(context);
        }
    }
}