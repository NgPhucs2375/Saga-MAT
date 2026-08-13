using System;
using Onion.CleanArchitecture.Domain.Events;
namespace Onion.CleanArchitecture.Domain.Events
{record OrderAcceptCompletedResponse(
    Guid EventId,
    Guid OrderId, 
    Guid CustomerId, 
    bool IsSuccess, 
    string ErrorReason, 
    DateTime Timestamp
    ) : IOrderEvent;
}
