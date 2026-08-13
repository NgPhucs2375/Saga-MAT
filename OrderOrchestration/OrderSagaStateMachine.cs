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
        public State PendingApproval { get; private set; }
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
        public Event<ApproveRequestedEvent> ApproveRequested { get; private set; }
        public Event<RejectRequestedEvent> RejectRequested { get; private set; }
        public Event<OrderCompletedEvent> OrderCompleted { get; private set; }
        public Event<OrderCompleteFailedEvent> OrderCompleteFailed { get; private set; }
        public Event<InventoryReleasedEvent> InventoryReleased { get; private set; }
        public Event<ReleaseInventoryFailedEvent> InventoryReleasedFailed { get; private set; }
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
            Event(() => ApproveRequested, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => RejectRequested, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderCompleted, x => x.CorrelateById(m => m.Message.OrderId));
            Event(() => OrderCompleteFailed, x => x.CorrelateById(m => m.Message.OrderId));
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
                    .TransitionTo(PendingApproval),
                When(OrderValidationFailed)
                    .Activity(x => x.OfType<OrderValidationFailedActivity>())
                    .TransitionTo(Rejected));

            During(PendingApproval,
                When(ApproveRequested)
                    .Activity(x => x.OfType<SendApproveCommandActivity>()),
                When(RejectRequested)
                    .Activity(x => x.OfType<SendRejectCommandActivity>()),
                When(OrderAccepted)
                    .Activity(x => x.OfType<OrderAcceptedActivity>())
                    .TransitionTo(Completing),
                When(OrderAcceptFailed)
                            .Activity(x => x.OfType<ReleaseInventoryCompensateActivity>())
                            .TransitionTo(CompensatingRelease)          
            );

            During(Completing,
                When(OrderCompleted)
                    .Activity(x => x.OfType<OrderCompletedActivity>())
                    .TransitionTo(Completed),
                When(OrderCompleteFailed)
                    .Then(x => x.Saga.ErrorReason = x.Message.ErrorReason)
                        .Activity(x => x.OfType<ReleaseInventoryCompensateActivityForCompleteFailed>())
                        .TransitionTo(CompensatingRelease)
            );


            During(CompensatingRelease,
                When(InventoryReleased)
                    .Activity(x => x.OfType<CancelOrderCompensateActivity>())
                    .TransitionTo(CompensatingCancel),
                When(InventoryReleasedFailed)
                    .Then(ctx => ctx.Publish(new OrderAcceptFailedResponse(
                        NewId.NextGuid(),
                        ctx.Saga.CorrelationId,
                        ctx.Saga.CustomerId,
                        ctx.Message.ErrorReason,
                        new NotificationPayLoad(
                            ctx.Saga.CustomerId,    
                            "Đơn hàng bị từ chối",
                            "Bồi hoàn tồn kho thất bại: " + ctx.Message.ErrorReason,
                            "Error",
                            ctx.Saga.CorrelationId,
                            DateTime.UtcNow),
                        DateTime.UtcNow)))
                    .TransitionTo(Rejected));

            During(CompensatingCancel,
                When(OrderCancelled)
                    .Then(ctx => ctx.Publish(new OrderAcceptFailedResponse(
                        NewId.NextGuid(),
                        ctx.Saga.CorrelationId,
                        ctx.Saga.CustomerId,
                        ctx.Saga.ErrorReason ?? "Đơn hàng đã bị từ chối",
                        new NotificationPayLoad(
                            ctx.Saga.CustomerId,
                            "Đơn hàng đã bị từ chối",
                            "Đơn hàng của bạn đã bị từ chối.",
                            "Error",
                            ctx.Saga.CorrelationId,
                            DateTime.UtcNow
                        ),
                        DateTime.UtcNow
                    )))
                    .TransitionTo(Rejected),
                When(CancelOrderFailed)
                    .Then(ctx => ctx.Publish(new OrderAcceptFailedResponse(
                        NewId.NextGuid(),
                        ctx.Saga.CorrelationId,
                        ctx.Saga.CustomerId,
                        ctx.Message.ErrorReason,
                        new NotificationPayLoad(
                            ctx.Saga.CustomerId,
                            "Đơn hàng bị từ chối",
                            "Bồi hoàn hủy đơn thất bại: " + ctx.Message.ErrorReason,
                            "Error",
                            ctx.Saga.CorrelationId,
                            DateTime.UtcNow),
                        DateTime.UtcNow)))
                    .TransitionTo(Rejected));
        }
    }
}