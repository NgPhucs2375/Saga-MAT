using Automatonymous;
using MassTransit;
using Onion.CleanArchitecture.Domain.Entities;
using Onion.CleanArchitecture.Domain.Events;
using System.Text.Json;

namespace OrderOrchestratorService
{
    public class OrderSagaStateMachine : MassTransitStateMachine<OrderState>
    {
        // === STATES === //
        public State Submitted { get; private set; }
        public State Validating { get; private set; }
        public State Accepting { get; private set; }
        public State Completing { get; private set; }
        public State Completed { get; private set; }
        public State Rejected { get; private set; }

        // === EVENTS === //
        public Event<OrderCreatedEvent> OrderCreated { get; private set; }
        public Event<OrderValidatedEvent> Validated { get; private set; }
        public Event<OrderValidationFailedEvent> ValidationFailed { get; private set; }
        public Event<OrderAcceptedEvent> Accepted { get; private set; }
        public Event<OrderAcceptFailedEvent> AcceptanceFailed { get; private set; }
        public Event<OrderCompletedEvent> CompletedEvent { get; private set; }
        public Event<OrderCompleteFailedEvent> CompletionFailed { get; private set; }
        public Event<OrderCancelledEvent> Cancelled { get; private set; }
        public Event<OrderTimeoutExpiredEvent> TimeoutExpired { get; private set; }

        // === ACTIVITIES === //
        public Activity<OrderState, OrderCreatedEvent> ValidateOrderActivity { get; set; }
        public Activity<OrderState, OrderValidatedEvent> SendAcceptOrderActivity { get; set; }
        public Activity<OrderState, OrderValidationFailedEvent> PublishRejectActivity { get; set; }
        public Activity<OrderState, OrderAcceptedEvent> SendCompleteOrderActivity { get; set; }
        public Activity<OrderState, OrderAcceptFailedEvent> PublishRejectActivity2 { get; set; }
        public Activity<OrderState, OrderCompletedEvent> PublishCompleteSuccessActivity { get; set; }
        public Activity<OrderState, OrderCompleteFailedEvent> CompensateOrderActivity { get; set; }
        public Activity<OrderState, OrderCancelledEvent> PublishCancelledActivity { get; set; }
        public Activity<OrderState, OrderTimeoutExpiredEvent> TimeoutCompensateActivity { get; set; }

        // === CONSTRUCTOR === //
        public OrderSagaStateMachine()
        {     public OrderSagaStateMachine()
        {
            Event(() => OrderCreated,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => Validated,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => ValidationFailed,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => Accepted,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => AcceptanceFailed,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => CompletedEvent,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => CompletionFailed,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => Cancelled,
                x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => TimeoutExpired,
                x => x.CorrelateById(m => m.Message.OrderId));

            // === Instance State === //
            InstanceState(x => x.CurrentState);

                Activity(() => ValidateOrderActivity);
                Activity(() => SendAcceptOrderActivity);
                Activity(() => PublishRejectActivity);
                Activity(() => SendCompleteOrderActivity);
                Activity(() => PublishCompleteSuccessActivity);
                Activity(() => CompensateOrderActivity);
                Activity(() => PublishCancelledActivity);
                Activity(() => TimeoutCompensateActivity);

            // === FLOW === //
            Initially(
                When(OrderCreated)
                    .Activity(x => x.ValidateOrderActivity)
                    .TransitionTo(Validating)
            );

            During(Validating,
                When(Validated)
                    .Activity(x => x.SendAcceptOrderActivity)
                    .TransitionTo(Accepting),
                When(ValidationFailed)
                    .Activity(x => x.PublishRejectActivity)
                    .TransitionTo(Rejected)
            );

            During(Accepting,
                When(Accepted)
                    .Activity(x => x.SendCompleteOrderActivity)
                    .TransitionTo(Completing),
                When(AcceptanceFailed)
                    .Activity(x => x.PublishRejectActivity2)
                    .TransitionTo(Rejected)
            );

            During(Completing,
                When(CompletedEvent)
                    .Activity(x => x.PublishCompleteSuccessActivity)
                    .TransitionTo(Completed),
                When(CompletionFailed)
                    .Activity(x => x.CompensateOrderActivity)
                    .TransitionTo(Rejected)
            );

            DuringAny(
                When(Cancelled)
                    .Activity(x => x.PublishCancelledActivity)
                    .TransitionTo(Rejected),
                When(TimeoutExpired)
                    .Activity(x => x.TimeoutCompensateActivity)
                    .TransitionTo(Rejected)
            );

        }
    }
            // === Correlation === //
    }
    }
