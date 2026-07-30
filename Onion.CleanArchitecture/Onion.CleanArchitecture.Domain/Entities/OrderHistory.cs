using System;
using Onion.CleanArchitecture.Domain.Common;
using Onion.CleanArchitecture.Domain.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class OrderHistory : AuditableBaseEntity
    {
        public Guid HistoryId { get; set; }
        public Guid OrderId { get; set;}
        public string ConsumerName { get; set; }
        public HistoryStatus Status { get; set; }
        public string EventType { get; set; }
        public string Message { get; set; }

    }
}