using MassTransit;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderOrchestration.Activities
{
    public class OrderCreatedActivity : IStateMachineActivity<OrderState, OrderCreatedEvent>
    {
        private static readonly Uri ValidationQueueUri = new("queue:order-validation-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderCreatedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderCreatedEvent> context,
            IBehavior<OrderState, OrderCreatedEvent> next)
        {
            context.Saga.CorrelationId = context.Message.OrderId;
            context.Saga.CustomerId = context.Message.CustomerId;
            context.Saga.TotalAmount = context.Message.TotalAmount;
            context.Saga.Items = context.Message.Items;
            context.Saga.CreatedAt = DateTime.UtcNow;

            await context.Send(
                ValidationQueueUri,
                new ValidateOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.Items,
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderCreatedEvent, TException> context,
            IBehavior<OrderState, OrderCreatedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderValidatedActivity : IStateMachineActivity<OrderState, OrderValidatedEvent>
    {
        private static readonly Uri AcceptQueueUri = new("queue:order-accept-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderValidatedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderValidatedEvent> context,
            IBehavior<OrderState, OrderValidatedEvent> next)
        {
            await context.Send(
                AcceptQueueUri,
                new AcceptOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderValidatedEvent, TException> context,
            IBehavior<OrderState, OrderValidatedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderValidationFailedActivity : IStateMachineActivity<OrderState, OrderValidationFailedEvent>
    {
        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderValidationFailedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderValidationFailedEvent> context,
            IBehavior<OrderState, OrderValidationFailedEvent> next)
        {
            await context.Publish(
                new OrderSubmitFailedResponse(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.ErrorMessage,
                    new NotificationPayLoad(
                        context.Message.CustomerId,
                        "Đơn hàng bị từ chối",
                        context.Message.ErrorMessage,
                        "Error",
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderValidationFailedEvent, TException> context,
            IBehavior<OrderState, OrderValidationFailedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderAcceptedActivity : IStateMachineActivity<OrderState, OrderAcceptedEvent>
    {
        private static readonly Uri CompleteQueueUri = new("queue:order-complete-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderAcceptedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderAcceptedEvent> context,
            IBehavior<OrderState, OrderAcceptedEvent> next)
        {
            await context.Send(
                CompleteQueueUri,
                new CompleteOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Saga.Items,
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderAcceptedEvent, TException> context,
            IBehavior<OrderState, OrderAcceptedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderAcceptFailedActivity : IStateMachineActivity<OrderState, OrderAcceptFailedEvent>
    {
        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderAcceptFailedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderAcceptFailedEvent> context,
            IBehavior<OrderState, OrderAcceptFailedEvent> next)
        {
            await context.Publish(
                new OrderAcceptFailedResponse(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.ErrorReason,
                    new NotificationPayLoad(
                        context.Message.CustomerId,
                        "Đơn hàng bị từ chối",
                        context.Message.ErrorReason,
                        "Error",
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderAcceptFailedEvent, TException> context,
            IBehavior<OrderState, OrderAcceptFailedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderCompletedActivity : IStateMachineActivity<OrderState, OrderCompletedEvent>
    {
        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderCompletedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderCompletedEvent> context,
            IBehavior<OrderState, OrderCompletedEvent> next)
        {
            await context.Publish(
                new OrderCompleteSuccessResponse(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    new NotificationPayLoad(
                        context.Message.CustomerId,
                        "Đơn hàng hoàn tất",
                        "Đơn hàng của bạn đã hoàn tất thành công.",
                        "Success",
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderCompletedEvent, TException> context,
            IBehavior<OrderState, OrderCompletedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderCompleteFailedActivity : IStateMachineActivity<OrderState, OrderCompleteFailedEvent>
    {
        private static readonly Uri CancelQueueUri = new("queue:order-cancel-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderCompleteFailedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderCompleteFailedEvent> context,
            IBehavior<OrderState, OrderCompleteFailedEvent> next)
        {
            await context.Send(
                CancelQueueUri,
                new CancelOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.ErrorReason,
                    DateTime.UtcNow));

            await context.Publish(
                new OrderCompleteFailedResponse(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.ErrorReason,
                    new NotificationPayLoad(
                        context.Message.CustomerId,
                        "Đơn hàng thất bại",
                        context.Message.ErrorReason,
                        "Error",
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderCompleteFailedEvent, TException> context,
            IBehavior<OrderState, OrderCompleteFailedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderTimeoutExpiredActivity : IStateMachineActivity<OrderState, OrderTimeoutExpiredEvent>
    {
        private static readonly Uri CancelQueueUri = new("queue:order-cancel-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderTimeoutExpiredActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderTimeoutExpiredEvent> context,
            IBehavior<OrderState, OrderTimeoutExpiredEvent> next)
        {
            await context.Send(
                CancelQueueUri,
                new CancelOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Saga.CustomerId,
                    "Hết thời gian xử lý đơn hàng (auto timeout)",
                    DateTime.UtcNow));

            await context.Publish(
                new OrderAcceptFailedResponse(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Saga.CustomerId,
                    "Timeout",
                    new NotificationPayLoad(
                        context.Saga.CustomerId,
                        "Đơn hàng bị từ chối",
                        "Đơn hàng của bạn đã bị từ chối tự động do quá thời gian xử lý.",
                        "Warning",
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderTimeoutExpiredEvent, TException> context,
            IBehavior<OrderState, OrderTimeoutExpiredEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }
}
