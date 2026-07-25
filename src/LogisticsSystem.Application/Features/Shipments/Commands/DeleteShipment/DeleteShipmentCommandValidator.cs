using FluentValidation;

namespace LogisticsSystem.Application.Features.Shipments.Commands.DeleteShipment
{
    public class DeleteShipmentCommandValidator : AbstractValidator<DeleteShipmentCommand>
    {
        public DeleteShipmentCommandValidator()
        {
            RuleFor(x=>x.Id)
                .NotEmpty();
        }
    }
}
