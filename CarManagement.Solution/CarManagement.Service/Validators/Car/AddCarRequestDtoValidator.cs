using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators.Car;

public class AddCarRequestDtoValidator : Validator<AddCarRequestDto>
{
    public AddCarRequestDtoValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty().WithMessage("Make is required")
            .MaximumLength(50).WithMessage("Make cannot exceed 50 characters");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required")
            .MaximumLength(50).WithMessage("Model cannot exceed 50 characters");

        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("Year is required")
            .GreaterThan(0).WithMessage("Year must be greater than 0");

        RuleFor(x => x.StockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Stock level must be greater than or equal to 0");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be greater than or equal to 0");
    }
}
