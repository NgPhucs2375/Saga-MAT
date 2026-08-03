using System;
using MassTransit;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class OrderState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CustomerId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; } 
        public string Items { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}