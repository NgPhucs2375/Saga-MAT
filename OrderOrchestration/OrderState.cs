using MassTransit;
using Onion.CleanArchitecture.Domain.Events;

namespace OrderOrchestration
{
    /// <summary>
    /// Saga instance lưu trạng thái quy trình đơn hàng trong PostgreSQL (bảng OrderState).
    /// CorrelationId = OrderId (PK).
    /// </summary>
    public class OrderState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int StepsCompleted { get; set; }
        public string ErrorReason { get; set; } = string.Empty;
    }
}
