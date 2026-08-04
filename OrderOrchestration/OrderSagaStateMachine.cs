using MassTransit;
using Onion.CleanArchitecture.Domain.Events;
using OrderOrchestration.Activities;

namespace OrderOrchestration
{
    /// <summary>
    /// Saga orchestrate: OrderCreated -> Validate -> Accept -> Complete -> Completed
    /// Bất kỳ bước nào fail -> Rejected. Timeout (DuringAny) -> Compensate -> Rejected.
    /// LIFO compensation: Complete fail -> ReleaseInventory -> Cancel -> Rejected.
    /// </summary>
    public class OrderSagaStateMachine : MassTransitStateMachine<OrderState>
    {
        public State Submitted { get; private set; }
        public State Validating { get; private set; }
        public State Accepting { get; private set; }
        public State Completing { get; private set; }
        public State Completed { get; private set; }
        public State Rejected { get; private set; }
        public State CompensatingRelease { get; private set; }
        public State CompensatingCancel { get; private set; }

        public Event<OrderCreatedEvent> OrderCreated { get; private set; }
        public Event<OrderValidatedEvent> OrderValidated { get; private set; }
        public Event<OrderValidationFailedEvent> OrderValidationFailed { get; private set; }
        public Event<OrderAcceptedEvent> OrderAccepted { get; private set; }
        public Event<OrderAcceptFailedEvent> OrderAcceptFailed { get; private set; }
        public Event<OrderCompletedEvent> OrderCompleted { get; private set; }
        public Event<OrderCompleteFailedEvent> OrderCompleteFailed { get; private set; }
        public Event<OrderTimeoutExpiredEvent> OrderTimeoutExpired { get; private set; }
        public Event<InventoryReleasedEvent> InventoryReleased { get; private set; }
        public Event<InventoryReleasedFailedEvent> InventoryReleasedFailed { get; private set; }
        public Event<OrderCancelledEvent> OrderCancelled { get; private set; }
        public Event<CancelOrderFailedEvent> CancelOrderFailed { get; private set; }

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
            Event(() => InventoryReleased, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => InventoryReleasedFailed, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderCancelled, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => CancelOrderFailed, x => x.CorrelateById(m => m.Message.OrderId));

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
                    .Then(x => x.Saga.StepsCompleted = 1)
                    .Activity(x => x.OfType<OrderAcceptedActivity>())
                    .TransitionTo(Completing),
                When(OrderAcceptFailed)
                    .Activity(x => x.OfType<OrderAcceptFailedActivity>())
                    .Then(x => x.Saga.ErrorReason = x.Message.ErrorReason)
                    .TransitionTo(Rejected));

            During(Completing,
                When(OrderCompleted)
                    .Then(x => x.Saga.StepsCompleted = 2)
                    .Activity(x => x.OfType<OrderCompletedActivity>())
                    .TransitionTo(Completed),
                When(OrderCompleteFailed)
                    .Activity(x => x.OfType<OrderCompleteFailedActivity>())
                    .Then(x => x.Saga.ErrorReason = x.Message.ErrorReason)
                    .IfElse(
                        x => x.Saga.StepsCompleted >= 1,
                        then => then
                            .Activity(x => x.OfInstanceType<ReleaseInventoryCompensateActivity>())
                            .TransitionTo(CompensatingRelease),
                        @else => @else.TransitionTo(Rejected)));

            DuringAny(
                When(OrderTimeoutExpired)
                    .Activity(x => x.OfType<OrderTimeoutExpiredActivity>())
                    .IfElse(
                        x => x.Saga.StepsCompleted >= 1,
                        then => then
                            .Activity(x => x.OfInstanceType<ReleaseInventoryCompensateActivity>())
                            .TransitionTo(CompensatingRelease),
                        @else => @else.TransitionTo(Rejected)));

            During(CompensatingRelease,
                When(InventoryReleased)
                    .Activity(x => x.OfInstanceType<CancelOrderCompensateActivity>())
                    .TransitionTo(CompensatingCancel),
                When(InventoryReleasedFailed)
                    .TransitionTo(Rejected));

            During(CompensatingCancel,
                When(OrderCancelled)
                    .TransitionTo(Rejected),
                When(CancelOrderFailed)
                    .TransitionTo(Rejected));
        }
    }
}