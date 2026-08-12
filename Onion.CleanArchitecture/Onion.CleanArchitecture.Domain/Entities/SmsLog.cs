using System;
using Onion.CleanArchitecture.Domain.Common;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class SmsLog : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; }
        public string Content { get; set; }
        public string Status { get; set; }
        public string ProviderResponse { get; set; }
        public int RetryCount { get; set; }
        public DateTime? SentAt { get; set; }
    }
}