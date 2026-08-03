using MassTransit;
using Onion.CleanArchitecture.Domain.Events;
using OrderOrchestration.Activities;

namespace OrderOrchestration
{
    /// <summary>
    /// Saga orchestrate: OrderCreated -> Validate -> Accept -> Complete -> Completed
    /// Bất kỳ bước nào fail -> Rejected. Timeout (DuringAny) -> Compensate -> Rejected.
    /// </summary>
    public class OrderSagaStateMachine : MassTransitStateMachine<OrderState>
    {
        public State Submitted { get; private set; }
        public State Validating { get; private set; }
        public State Accepting { get; private set; }
        public State Completing { get; private set; }
        public State Completed { get; private set; }
        public State Rejected { get; private set; }

        public Event<OrderCreatedEvent> OrderCreated { get; private set; }
        public Event<OrderValidatedEvent> OrderValidated { get; private set; }
        public Event<OrderValidationFailedEvent> OrderValidationFailed { get; private set; }
        public Event<OrderAcceptedEvent> OrderAccepted { get; private set; }
        public Event<OrderAcceptFailedEvent> OrderAcceptFailed { get; private set; }
        public Event<OrderCompletedEvent> OrderCompleted { get; private set; }
        public Event<OrderCompleteFailedEvent> OrderCompleteFailed { get; private set; }
        public Event<OrderTimeoutExpiredEvent> OrderTimeoutExpired { get; private set; }

        public OrderSagaStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => OrderCreated, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderValidated, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderValidationFailed, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderAccepted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderAcceptFailed, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderCompleted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderCompleteFailed, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderTimeoutExpired, x => x.CorrelateById(m => m.Message.OrderId));

            Initially(
                When(OrderCreated)
                    .Activity(x => x.OfType<OrderCreatedActivity>())
                    .TransitionTo(Validating));

            During(Validating,
                When(OrderValidated)
                    .Activity(x => x.OfType<OrderValidatedActivity>())
                    .TransitionTo(Accepting),
                When(OrderValidationFailed)
                    .Activity(x => x.OfType<OrderValidationFailedActivity>())
                    .TransitionTo(Rejected));

            During(Accepting,
                When(OrderAccepted)
                    .Activity(x => x.OfType<OrderAcceptedActivity>())
                    .TransitionTo(Completing),
                When(OrderAcceptFailed)
                    .Activity(x => x.OfType<OrderAcceptFailedActivity>())
                    .TransitionTo(Rejected));

            During(Completing,
                When(OrderCompleted)
                    .Activity(x => x.OfType<OrderCompletedActivity>())
                    .TransitionTo(Completed),
                When(OrderCompleteFailed)
                    .Activity(x => x.OfType<OrderCompleteFailedActivity>())
                    .TransitionTo(Rejected));

            DuringAny(
                When(OrderTimeoutExpired)
                    .Activity(x => x.OfType<OrderTimeoutExpiredActivity>())
                    .TransitionTo(Rejected));
        }
    }
}
