using CarManagement.Service.DTOs.Car;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators.Car;

/// <summary>
/// Validator for <see cref="UpdateCarStockLevelRequestDto"/>.
/// </summary>
public sealed class UpdateCarStockLevelRequestDtoValidator : Validator<UpdateCarStockLevelRequestDto>
{
    public UpdateCarStockLevelRequestDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.StockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Stock level must be greater than or equal to 0");
    }
}
