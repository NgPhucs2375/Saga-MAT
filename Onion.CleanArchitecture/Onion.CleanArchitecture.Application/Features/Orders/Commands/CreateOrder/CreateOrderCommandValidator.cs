using FluentValidation;

namespace Onion.CleanArchitecture.Apllication.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(p => p.ShippingAddress)
                .NotEmpty().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.Items)
                .NotEmpty().WithMessage("Order must contain at least one item.");
        }
    }
}
