using FluentValidation;
using Sanzu.Core.Models.Requests;

namespace Sanzu.Core.Validators;

public sealed class CreateAgencyAccountRequestValidator : AbstractValidator<CreateAgencyAccountRequest>
{
    public CreateAgencyAccountRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.AgencyName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(255);
    }
}
