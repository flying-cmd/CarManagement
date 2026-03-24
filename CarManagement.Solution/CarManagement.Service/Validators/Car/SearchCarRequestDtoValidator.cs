using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators.Car;

public class SearchCarRequestDtoValidator : Validator<SearchCarRequestDto>
{
    public SearchCarRequestDtoValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Make), () =>
        {
            RuleFor(x => x.Make)
                .MaximumLength(50).WithMessage("Make cannot exceed 50 characters");
        });

        When(x => !string.IsNullOrEmpty(x.Model), () =>
        {
            RuleFor(x => x.Model)
                .MaximumLength(50).WithMessage("Model cannot exceed 50 characters");
        });

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100");
    }
}
