using System;
using Onion.CleanArchitecture.Domain.Common;

namespace Onion.CleanArchitecture.Domain.Entities
{
    public class Notification : AuditableBaseEntity
    {
        public Guid NotifyId { get; set; }
        public Guid OrderId { get; set; }
        public Guid TargetUserId { get; set; } //ID nguowif nhanaj trn UI Client
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SendAt { get; set; } = DateTime.Now; // thoi gian day qua SignalR
    }
}