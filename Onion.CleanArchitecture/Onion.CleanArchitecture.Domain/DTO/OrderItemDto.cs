using System;

namespace Onion.CleanArchitecture.Domain.Events
{
    public record OrderItemDto(
        Guid ProductId,
        int Quantity,
        decimal UnitPrice
    );

}