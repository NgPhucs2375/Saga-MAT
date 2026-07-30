using System;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Enums;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class OrderTimer : AuditableBaseEntity
    {
        public Guid TimerId { get; set; }
        public Guid OrderId { get; set; }
        public DateTime Timeout { get; set; }
        public TargetStatus Status { get; set; }
        public TimerStatus TimerStatus { get; set; }
    }
}