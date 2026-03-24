using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators;

public sealed class RemoveCarRequestDtoValidator : Validator<RemoveCarRequestDto>
{
    public RemoveCarRequestDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}
