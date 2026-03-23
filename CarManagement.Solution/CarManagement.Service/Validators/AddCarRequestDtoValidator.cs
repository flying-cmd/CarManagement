using CarManagement.Service.DTOs;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators;

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

        RuleFor(x => x.Colour)
            .NotEmpty().WithMessage("Colour is required")
            .MaximumLength(50).WithMessage("Colour cannot exceed 50 characters");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be greater than or equal to 0");

        RuleFor(x => x.StockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Stock level must be greater than or equal to 0");
    }
}
