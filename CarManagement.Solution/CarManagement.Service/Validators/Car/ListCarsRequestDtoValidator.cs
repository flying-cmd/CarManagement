using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators.Car;

/// <summary>
/// Validator for <see cref="ListCarsRequestDto"/>.
/// </summary>
public class ListCarsRequestDtoValidator : Validator<ListCarsRequestDto>
{
    public ListCarsRequestDtoValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}
