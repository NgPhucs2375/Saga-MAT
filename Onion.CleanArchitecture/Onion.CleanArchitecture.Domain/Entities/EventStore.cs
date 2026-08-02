using System;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Enums;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class EventStore : AuditableBaseEntity
    {
        public Guid StoreId { get; set; }
        public string EventType { get; set; }
        public string PayLoad { get; set; }
        public EventStatus Status { get; set; }
        public int RetryCount { get; set; }
        public string ErrorMessage { get; set; }
        public Guid CorrelationId { get; set; }
        public int SequenceNumber {get; set;}
        public DateTime? ProcessedAt { get; set; }
    }
}