using CarManagement.Service.DTOs.Auth;
using FastEndpoints;
using FluentValidation;

namespace CarManagement.Service.Validators.Auth;

/// <summary>
/// Validator for <see cref="LoginRequestDto"/>.
/// </summary>
public class LoginRequestDtoValidator : Validator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email is invalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
