using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators.Car;

/// <summary>
/// Validator for <see cref="RemoveCarRequestDto"/>.
/// </summary>
public sealed class RemoveCarRequestDtoValidator : Validator<RemoveCarRequestDto>
{
    public RemoveCarRequestDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}
