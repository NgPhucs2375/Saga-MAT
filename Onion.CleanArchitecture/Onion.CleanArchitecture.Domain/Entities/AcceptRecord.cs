using System;
using Onion.CleanArchitecture.Domain.Common;

namespace Onion.CleanArchitecture.Domain.Entities;

public class AcceptRecord : AuditableBaseEntity
{
    public Guid RecordId { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public string Action {get; set;}
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AcceptedAt { get; set; }
}