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
                        context.Saga.CorrelationId,
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

            // Thông báo bước Accept thành công -> NotificationService đẩy SignalR
            await context.Publish(new OrderAcceptSuccessResponse(
                NewId.NextGuid(),
                context.Message.OrderId,
                context.Message.CustomerId,
                new NotificationPayLoad(
                    context.Message.CustomerId,
                    "Đơn hàng đã được duyệt",
                    "Đơn hàng của bạn đã được duyệt và đang được xử lý.",
                    "Success",
                    context.Saga.CorrelationId,
                    DateTime.UtcNow),
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
                        context.Saga.CorrelationId,
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

    public class SendApproveCommandActivity : IStateMachineActivity<OrderState, ApproveRequestedEvent>
    {
        private static readonly Uri ApproveQueueUri = new("queue:order-approve-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(SendApproveCommandActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, ApproveRequestedEvent> context,
            IBehavior<OrderState, ApproveRequestedEvent> next)
        {
            await context.Send(
                ApproveQueueUri,
                new ApproveOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, ApproveRequestedEvent, TException> context,
            IBehavior<OrderState, ApproveRequestedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class SendRejectCommandActivity : IStateMachineActivity<OrderState, RejectRequestedEvent>
    {
        private static readonly Uri RejectQueueUri = new("queue:order-reject-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(SendRejectCommandActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, RejectRequestedEvent> context,
            IBehavior<OrderState, RejectRequestedEvent> next)
        {
            await context.Send(
                RejectQueueUri,
                new RejectOrderCommand(
                    Guid.NewGuid(),
                    context.Message.OrderId,
                    context.Message.CustomerId,
                    context.Message.Reason,
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, RejectRequestedEvent, TException> context,
            IBehavior<OrderState, RejectRequestedEvent> next)
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
                        context.Saga.CorrelationId,
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await context.Publish(
                new SendSmsCommand(
                    NewId.NextGuid(),
                    context.Message.OrderId,
                    PhoneNumber: "0961100165",
                    Content:$"Đơn hàng #{context.Message.OrderId.ToString()[..8]} đã được tạo và thanh toán thành công. Cảm ơn bạn đã mua sắm tại cửa hàng của chúng tôi.",
                    DateTime.UtcNow)
                
            );

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, OrderCompletedEvent, TException> context,
            IBehavior<OrderState, OrderCompletedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }


    public class ReleaseInventoryCompensateActivity : IStateMachineActivity<OrderState,OrderAcceptFailedEvent>
    {
        private static readonly Uri ReleaseQueueUri = new("queue:release-inventory-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(ReleaseInventoryCompensateActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState,OrderAcceptFailedEvent> context,
            IBehavior<OrderState, OrderAcceptFailedEvent> next)
        {
            await SendReleaseInventoryCommandAsync(context);
            await next.Execute(context);
        }

        private async Task SendReleaseInventoryCommandAsync(BehaviorContext<OrderState,OrderAcceptFailedEvent> context)
        {
            await context.Send(
                ReleaseQueueUri,
                new ReleaseInventoryCommand(
                    NewId.NextGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CustomerId,
                    context.Saga.Items,
                    context.Saga.ErrorReason ?? "Compensate",
                    DateTime.UtcNow));
        }
        public Task Faulted<TException>(
                BehaviorExceptionContext<OrderState, OrderAcceptFailedEvent, TException> context,
                IBehavior<OrderState, OrderAcceptFailedEvent> next)
                where TException : Exception =>
                next.Faulted(context);
    }

    public class ReleaseInventoryCompensateActivityForCompleteFailed : IStateMachineActivity<OrderState, OrderCompleteFailedEvent>
    {
        private static readonly Uri ReleaseQueueUri = new("queue:release-inventory-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(ReleaseInventoryCompensateActivityForCompleteFailed));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, OrderCompleteFailedEvent> context,
            IBehavior<OrderState, OrderCompleteFailedEvent> next)
        {
            await SendReleaseInventoryCommandAsync(context);
            await next.Execute(context);
        }

        private async Task SendReleaseInventoryCommandAsync(BehaviorContext<OrderState, OrderCompleteFailedEvent> context)
        {
            await context.Send(
                ReleaseQueueUri,
                new ReleaseInventoryCommand(
                    NewId.NextGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CustomerId,
                    context.Saga.Items,
                    context.Saga.ErrorReason ?? "Compensate",
                    DateTime.UtcNow));
        }
        public Task Faulted<TException>(
                BehaviorExceptionContext<OrderState, OrderCompleteFailedEvent, TException> context,
                IBehavior<OrderState, OrderCompleteFailedEvent> next)
                where TException : Exception =>
                next.Faulted(context);
    }


    public class CancelOrderCompensateActivity : IStateMachineActivity<OrderState, InventoryReleasedEvent>
    {
        private static readonly Uri CancelQueueUri = new("queue:order-cancel-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(CancelOrderCompensateActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, InventoryReleasedEvent> context,
            IBehavior<OrderState, InventoryReleasedEvent> next)
        {
            await SendCancelOrderCommandAsync(context);
            await next.Execute(context);
        }

        private async Task SendCancelOrderCommandAsync(BehaviorContext<OrderState, InventoryReleasedEvent> context)
        {
            await context.Send(
                CancelQueueUri,
                new CancelOrderCommand(
                    NewId.NextGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CustomerId,
                    context.Saga.ErrorReason ?? "Compensate",
                    DateTime.UtcNow));
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, InventoryReleasedEvent, TException> context,
            IBehavior<OrderState, InventoryReleasedEvent> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class OrderValidationFaultedActivity : IStateMachineActivity<OrderState, Fault<ValidateOrderCommand>>
    {
        public void Probe(ProbeContext context) => context.CreateScope(nameof(OrderValidationFaultedActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, Fault<ValidateOrderCommand>> context,
            IBehavior<OrderState, Fault<ValidateOrderCommand>> next)
        {
            var command = context.Message.Message;
            var errorMessage = context.Message.Exceptions.FirstOrDefault()?.Message
                ?? "Lỗi hệ thống khi xác thực đơn hàng!";

            await context.Publish(
                new OrderSubmitFailedResponse(
                    Guid.NewGuid(),
                    command.OrderId,
                    command.CustomerId,
                    errorMessage,
                    new NotificationPayLoad(
                        command.CustomerId,
                        "Đơn hàng bị từ chối",
                        errorMessage,
                        "Error",
                        context.Saga.CorrelationId,
                        DateTime.UtcNow),
                    DateTime.UtcNow));

            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, Fault<ValidateOrderCommand>, TException> context,
            IBehavior<OrderState, Fault<ValidateOrderCommand>> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class AcceptOrderFaultCompensateActivity : IStateMachineActivity<OrderState, Fault<AcceptOrderCommand>>
    {
        private static readonly Uri ReleaseQueueUri = new("queue:release-inventory-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(AcceptOrderFaultCompensateActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, Fault<AcceptOrderCommand>> context,
            IBehavior<OrderState, Fault<AcceptOrderCommand>> next)
        {
            await context.Send(
                ReleaseQueueUri,
                new ReleaseInventoryCommand(
                    NewId.NextGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CustomerId,
                    context.Saga.Items,
                    context.Saga.ErrorReason ?? "Compensate",
                    DateTime.UtcNow));
            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, Fault<AcceptOrderCommand>, TException> context,
            IBehavior<OrderState, Fault<AcceptOrderCommand>> next)
            where TException : Exception =>
            next.Faulted(context);
    }

    public class CompleteOrderFaultCompensateActivity : IStateMachineActivity<OrderState, Fault<CompleteOrderCommand>>
    {
        private static readonly Uri ReleaseQueueUri = new("queue:release-inventory-queue");

        public void Probe(ProbeContext context) => context.CreateScope(nameof(CompleteOrderFaultCompensateActivity));

        public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);

        public async Task Execute(
            BehaviorContext<OrderState, Fault<CompleteOrderCommand>> context,
            IBehavior<OrderState, Fault<CompleteOrderCommand>> next)
        {
            await context.Send(
                ReleaseQueueUri,
                new ReleaseInventoryCommand(
                    NewId.NextGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CustomerId,
                    context.Saga.Items,
                    context.Saga.ErrorReason ?? "Compensate",
                    DateTime.UtcNow));
            await next.Execute(context);
        }

        public Task Faulted<TException>(
            BehaviorExceptionContext<OrderState, Fault<CompleteOrderCommand>, TException> context,
            IBehavior<OrderState, Fault<CompleteOrderCommand>> next)
            where TException : Exception =>
            next.Faulted(context);
    }
}
